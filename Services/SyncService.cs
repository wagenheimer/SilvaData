using SilvaData.Models;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Text.Json;

namespace SilvaData.Services
{
    public class SyncProgressReport
    {
        public string Texto { get; set; } = string.Empty;
        public string SubTexto { get; set; } = string.Empty;
        public string Texto2 { get; set; } = string.Empty;
        public int ProgressoAtual { get; set; }
        public int ProgressoTotal { get; set; }
        public int? SubAtual { get; set; }
        public int? SubTotal { get; set; }
    }

    public class LoteResultado
    {
        public int? LoteId { get; set; }
        public ISIWebServiceResult Resultado { get; set; } = new();
    }

    public class SyncService
    {
        private readonly ISIWebService _webService;
        private IProgress<SyncProgressReport>? _progressReporter;
        private int _progressoAtual;
        private int _totalProgresso;
        private DateTime _ultimoReportProgress = DateTime.MinValue;
        private string _ultimoTextoProgress = string.Empty;
        // long em vez de DateTime: DateTime não é atômico em ARM32 (64 bits em dois acessos),
        // o que causaria race condition nos loops paralelos de download de imagens.
        private long _ultimoReportSubTaskTicks = DateTime.MinValue.Ticks;
        // Contador compartilhado entre threads paralelas — sempre usar Interlocked para R/W.
        private int downloadNuvem = 0;
        // Rastreia downloads de imagem atualmente em voo (não apenas enfileirados no semaphore).
        // Permite exibir um contador real e garantir que "Concluído" só apareça quando tudo terminou.
        private int _pendingDownloads = 0;

        // Limita conexões HTTP simultâneas para não saturar a rede nem o servidor.
        private readonly SemaphoreSlim _imagesSemaphore = new(8, 8);

        // Throttle para atualizações de UI principais
        private readonly TimeSpan _uiThrottle = TimeSpan.FromMilliseconds(300);
        // Throttle para sub-tarefas paralelas — evita flood da UI thread quando dezenas de
        // downloads reportam progresso ao mesmo tempo via Progress<T>.
        private readonly TimeSpan _subTaskThrottle = TimeSpan.FromMilliseconds(150);
        private CancellationTokenSource? _cts;

        public SyncService(ISIWebService webService) => _webService = webService;

        public void Cancel() { try { _cts?.Cancel(); } catch { } }

        private void InitializeProgress(int totalSteps)
        {
            _totalProgresso = totalSteps > 0 ? totalSteps : 18;
            _progressoAtual = 0;
            _ultimoReportProgress = DateTime.MinValue;
            _ultimoTextoProgress = string.Empty;
            // Zera o throttle de sub-tarefas via Interlocked — seguro para ARM32.
            Interlocked.Exchange(ref _ultimoReportSubTaskTicks, DateTime.MinValue.Ticks);
            Interlocked.Exchange(ref _pendingDownloads, 0);
        }

        // Relatório para etapas principais (com throttle).
        // Sempre dispara quando o texto muda para que o label da UI seja atualizado imediatamente.
        private void ReportMainStep(string texto, string texto2 = "", bool incrementar = true)
        {
            if (incrementar)
                Interlocked.Increment(ref _progressoAtual);

            var now = DateTime.UtcNow;
            var textoMudou = texto != _ultimoTextoProgress;
            // Throttle: suprime envios redundantes no mesmo intervalo, exceto ao trocar etapa.
            if (!textoMudou && (now - _ultimoReportProgress) < _uiThrottle && _progressoAtual < _totalProgresso)
                return;

            _ultimoReportProgress = now;
            _ultimoTextoProgress = texto;
            var atual = Math.Min(_progressoAtual, _totalProgresso);
            _progressReporter?.Report(new SyncProgressReport
            {
                Texto = texto,
                SubTexto = string.Empty,
                Texto2 = texto2,
                ProgressoAtual = atual,
                ProgressoTotal = _totalProgresso,
                SubAtual = null,
                SubTotal = null
            });
        }

        // Relatório detalhado para sub-tarefas (imagens, lotes, etc.).
        // Thread-safe: compara e troca ticks via Interlocked, sem lock, para performance
        // em loops paralelos intensos (Parallel.ForEachAsync com 8–15 threads simultâneas).
        private void ReportSubTask(
            string mainText,
            string subText,
            int? subAtual,
            int? subTotal,
            string? detail = null,
            bool incrementar = false)
        {
            if (incrementar)
                Interlocked.Increment(ref _progressoAtual);

            // Sempre envia o report final para garantir que a barra de sub-progresso
            // chegue a 100% na UI — os intermediários podem ser descartados pelo throttle.
            var isFinal = subAtual.HasValue && subTotal.HasValue && subAtual.Value >= subTotal.Value;
            if (!isFinal)
            {
                var now = DateTime.UtcNow.Ticks;
                var last = Interlocked.Read(ref _ultimoReportSubTaskTicks);
                if (now - last < _subTaskThrottle.Ticks)
                    return;
                Interlocked.Exchange(ref _ultimoReportSubTaskTicks, now);
            }

            _progressReporter?.Report(new SyncProgressReport
            {
                Texto = mainText,
                SubTexto = subText,
                Texto2 = detail ?? string.Empty,
                ProgressoAtual = _progressoAtual,
                ProgressoTotal = _totalProgresso,
                SubAtual = subAtual,
                SubTotal = subTotal
            });
        }

        // Executa uma etapa não-crítica: absorve exceções de negócio/rede para que falhas
        // em dados secundários (modelos, regionais, notificações...) não abortem a sincronização.
        // OperationCanceledException sempre re-propaga — cancelamento do usuário é intencional.
        private async Task RunStep(Func<Task> step, string nome)
        {
            try
            {
                await step().ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Sync] Etapa '{nome}' falhou (continuando): {ex.Message}");
                SentryHelper.CaptureExceptionWithUser(ex, _webService.LoggedUser?.nome, nome);
            }
        }

        /// <summary>
        /// Ponto de entrada principal para o download de dados.
        /// </summary>
        public async Task<string> DownloadDataFromServer(IProgress<SyncProgressReport> progress)
        {
            _progressReporter = progress;
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                // ✅ Bloqueia haptic feedback durante a sincronização
                LoteFormAvaliacaoGalpao.IsLoadingData = true;

                token.ThrowIfCancellationRequested();
                var ultimaAtualizacao = Preferences.Get("lastsyncdatetime", DateTime.MinValue);
                var sincronizacaoCompleta = Preferences.Get("PrecisaSincronizacaoCompleta", true);

                // 16 etapas fixas + 1 se sincronização completa + 1 etapa final
                InitializeProgress(16 + (sincronizacaoCompleta ? 1 : 0) + 1);
                var loggedUser = _webService.LoggedUser ?? throw new InvalidOperationException("Usuário não está logado.");

                var parameters = new getDataDefaultParameters
                {
                    usuario = loggedUser.id,
                    dispositivoId = loggedUser.dispositivoId,
                    session = loggedUser.session,
                    idioma = LocalizationManager.LocManager.IdiomaParaWebService,
                    data = ultimaAtualizacao != DateTime.MinValue ? ultimaAtualizacao.ToString("yyyy-MM-dd") : ""
                };
                var jsonParameters = JsonConvert.SerializeObject(parameters).Replace("\"data\":\"\",", "");

                if (sincronizacaoCompleta)
                {
                    ReportMainStep(Traducao.ApagandoTabelasAntigas, incrementar: true);
                    await ManutencaoTabelas.DeletaTabelasAntigas().ConfigureAwait(false);
                    await ManutencaoTabelas.CriaOuAtualizaTabelas().ConfigureAwait(false);
                }

                // ── 1. Solo crítico: deve ser o primeiro ────────────────────────────────
                token.ThrowIfCancellationRequested();
                ReportMainStep(Traducao.BaixandoParâmetrosDaNuvem);
                await SyncParametrosAsync(jsonParameters).ConfigureAwait(false);

                // ── 2. Paralelo: críticos + independentes sem dependência entre si ───────
                // Os críticos (Propriedades, Lotes) e os não-críticos independentes
                // rodam ao mesmo tempo. Task.WhenAll só lança se um crítico falhar.
                token.ThrowIfCancellationRequested();
                ReportMainStep(Traducao.BaixandoPropriedadesDaNuvem);
                ReportMainStep(Traducao.BaixandoLotesFechadosDaNuvem);
                ReportMainStep(Traducao.BaixandoParâmetrosDosLotesDaNuvem);
                ReportMainStep(Traducao.BaixandoModelosDeISIMacro);
                ReportMainStep(Traducao.BaixandoProprietáriosDaNuvem);
                ReportMainStep(Traducao.BaixandoRegionaisDaNuvem);
                ReportMainStep(Traducao.BaixandoParâmetrosDasAtividadesDaNuvem);
                ReportMainStep(Traducao.BaixandoTipoDeAtividadesDaNuvem);
                ReportMainStep(Traducao.BaixandoNotificaçõesDaNuvem);
                ReportMainStep(Traducao.BaixandoPermissõesDaNuvem);
                await Task.WhenAll(
                    SyncPropriedadesAsync(jsonParameters),                                                              // crítico
                    SyncLotesFechadosAsync(parameters),                                                                 // crítico
                    SyncLotesAbertosAsync(parameters),                                                                  // crítico
                    RunStep(() => SyncModelosIsiMacroAsync(jsonParameters),          nameof(SyncModelosIsiMacroAsync)),
                    RunStep(() => SyncProprietariosAsync(jsonParameters),             nameof(SyncProprietariosAsync)),
                    RunStep(() => SyncRegionaisAsync(jsonParameters),                 nameof(SyncRegionaisAsync)),
                    RunStep(() => SyncTipoCorAtividadesAsync(jsonParameters),         nameof(SyncTipoCorAtividadesAsync)),
                    RunStep(() => SyncTipoAtividadesAsync(jsonParameters),            nameof(SyncTipoAtividadesAsync)),
                    RunStep(() => SyncNotificacoesAsync(jsonParameters),              nameof(SyncNotificacoesAsync)),
                    RunStep(() => SyncPermissoesAsync(jsonParameters),                nameof(SyncPermissoesAsync))
                ).ConfigureAwait(false);

                // ── 3. Sequencial: dependem do paralelo acima ────────────────────────────
                token.ThrowIfCancellationRequested();
                ReportMainStep(Traducao.BaixandoUnidadesEpidemiológicasDaNuvem);
                await RunStep(() => SyncUnidadesEpidemiologicasAsync(jsonParameters), nameof(SyncUnidadesEpidemiologicasAsync)).ConfigureAwait(false);

                // --- Limpeza de órfãos (Lotes sem Unidade Epidemiológica) ---
                // Agora é seguro fazer, pois as UEs já estão no banco.
                await Db.ExecuteAsync("delete from Lote where not exists (select * from UnidadeEpidemiologica ue where ue.idApp=lote.unidadeEpidemiologicaId)").ConfigureAwait(false);

                token.ThrowIfCancellationRequested();
                ReportMainStep(Traducao.BaixandoAtividadesDaNuvem);
                await RunStep(() => SyncAtividadesAsync(jsonParameters), nameof(SyncAtividadesAsync));

                // ── 4. Paralelo: formulários dependem dos lotes (etapa 2) ────────────────
                token.ThrowIfCancellationRequested();
                ReportMainStep(Traducao.BaixandoFormuláriosDosLotesDaNuvem);
                ReportMainStep(Traducao.BaixandoAvaliaçõesDoGalpãoDaNuvem);
                await Task.WhenAll(
                    RunStep(() => BaixaFormulariosLoteDaWeb(parameters),             nameof(BaixaFormulariosLoteDaWeb)),
                    RunStep(() => BaixaFormulariosAvaliacoesGalpaoDaWeb(parameters), nameof(BaixaFormulariosAvaliacoesGalpaoDaWeb))
                ).ConfigureAwait(false);

                // ── 5. Solo: cálculo local, depende de todos os formulários ──────────────
                token.ThrowIfCancellationRequested();
                ReportMainStep(Traducao.AtualizandoScoreDosLotes);
                await RunStep(() => AtualizaTodasMediasISIMacroComTransacao(), nameof(AtualizaTodasMediasISIMacroComTransacao));

                // Aguarda eventuais downloads ainda em voo antes de marcar como concluído.
                // Em condições normais _pendingDownloads já é 0 aqui; o loop é uma salvaguarda.
                while (_pendingDownloads > 0 && !token.IsCancellationRequested)
                {
                    ReportMainStep($"Aguardando {_pendingDownloads} download(s) pendente(s)...", incrementar: false);
                    await Task.Delay(200, token).ConfigureAwait(false);
                }

                ReportMainStep(Traducao.SincronizaçãoCompletada, incrementar: true);
                Preferences.Set("lastsyncdatetime", DateTime.Now);
                if (sincronizacaoCompleta) Preferences.Set("PrecisaSincronizacaoCompleta", false);

                return string.Empty;
            }
            catch (OperationCanceledException) { return Traducao.OperaçãoCancelada; }
            catch (Exception e)
            {
                Debug.WriteLine($"Falha na Sincronização: {e.Message}");
                SentryHelper.CaptureExceptionWithUser(e, _webService.LoggedUser?.nome, "DownloadDataFromServer");
                return e.Message;
            }
            finally
            {
                // ✅ Libera haptic feedback após o término ou erro
                LoteFormAvaliacaoGalpao.IsLoadingData = false;

                _cts?.Dispose();
                _cts = null;
            }

        }

        #region Métodos de Sincronização

        private async Task SyncParametrosAsync(string jsonParameters)
        {
            using var requestBody = new StringContent(EncryptDecrypt.Encrypt(jsonParameters));
            var result = await _webService.ExecutePostAndWaitResult(requestBody, "getParametrosGalpao").ConfigureAwait(false);
            if (!result.sucesso) throw new Exception($"Erro ao fazer download de parâmetros - {result.mensagem} {result.data}");
            var parametros = JsonConvert.DeserializeObject<Parametros>(result.data);
            if (parametros?.parametros == null) return;

            await Db.RunInTransactionAsync(nonAsyncConnection =>
            {
                foreach (var parametro in parametros.parametros)
                {
                    if (parametro.parametroCategoria != null && !string.IsNullOrEmpty(parametro.parametroCategoria.nome))
                    {
                        nonAsyncConnection.InsertOrReplace(new ParametroCategoria
                        {
                            id = parametro.parametroCategoria.id,
                            nome = parametro.parametroCategoria.nome,
                            ordem = parametro.parametroCategoria.ordem
                        });
                    }
                    nonAsyncConnection.InsertOrReplace(new Parametro
                    {
                        id = parametro.id,
                        nome = parametro.nome,
                        ordem = parametro.ordem,
                        exibir = parametro.exibir,
                        cliente = parametro.cliente,
                        dataUltimaAtualizacao = parametro.dataUltimaAtualizacao,
                        parametroTipo = parametro.parametroTipo,
                        tipoPreenchimento = parametro.tipoPreenchimento,
                        valorMaximo = parametro.valorMaximo,
                        valorMinimo = parametro.valorMinimo,
                        valorPadrao = parametro.valorPadrao,
                        parametroCategoriaId = parametro.parametroCategoria?.id ?? 0,
                        excluido = parametro.excluido,
                        required = parametro.required,
                        peso = parametro.peso,
                        Tipo = parametro.Tipo ?? 0,
                        qtdCampos = parametro.qtdCampos ?? 0,
                        qtdMinima = parametro.qtdMinima ?? 0,
                        campoTipo = parametro.campoTipo ?? string.Empty
                    });
                    foreach (var alternativa in parametro.alternativas)
                    {
                        if (string.IsNullOrEmpty(alternativa.descricao)) continue;
                        var normalizedImageName = ParametroAlternativasFromWebService.NormalizeImageFileName(alternativa.urlImagem);
                        if (!string.IsNullOrEmpty(alternativa.urlImagem))
                        {
                            Debug.WriteLine($"[Sync.ParametroImagem] Param={parametro.id} Alt={alternativa.id} raw='{alternativa.urlImagem}' normalized='{normalizedImageName}'");
                        }

                        nonAsyncConnection.Execute($"delete from ParametroAlternativas where idParametro={parametro.id} and id={alternativa.id}");
                        nonAsyncConnection.InsertOrReplace(new ParametroAlternativas
                        {
                            id = alternativa.id,
                            idParametro = parametro.id,
                            descricao = alternativa.descricao,
                            ordem = alternativa.ordem,
                            score = alternativa.score,
                            valorPadrao = alternativa.valorPadrao,
                            urlImagem = normalizedImageName,
                            excluido = alternativa.excluido
                        });
                    }
                }
                if (parametros.configParametros != null)
                {
                    nonAsyncConnection.Execute("delete from ConfigParametros");
                    nonAsyncConnection.Execute("delete from ParametroDiagnosticoTratamento");
                    nonAsyncConnection.Execute("delete from ParametroDiagnosticoTratamentoNomes");
                    nonAsyncConnection.Insert(new ConfigParametros
                    {
                        dataUltimaAtualizacao = DateTime.Now,
                        parametroTratamentoNomeProdutoId = parametros.configParametros.parametroTratamentoNomeProdutoId,
                        parametroTratamentoProdutoId = parametros.configParametros.parametroTratamentoProdutoId
                    });
                    var codigo = 0;
                    foreach (var tratamento in parametros.configParametros.diagnosticosTratamentos)
                    {
                        codigo++;
                        nonAsyncConnection.Insert(new ParametroDiagnosticoTratamento
                        {
                            idParametroDiagnostico = codigo,
                            dataUltimaAtualizacao = tratamento.dataUltimaAtualizacao,
                            diagnosticoEnfermidadeId = tratamento.diagnosticoEnfermidadeId,
                            tratamentoProdutoId = tratamento.tratamentoProdutoId
                        });
                        if (tratamento.produtosNomes != null)
                        {
                            foreach (var produto in tratamento.produtosNomes)
                            {
                                nonAsyncConnection.Insert(new ParametroDiagnosticoTratamentoNomes
                                {
                                    idParametroDiagnostico = codigo,
                                    produtoNomeId = produto.produtoNomeId
                                });
                            }
                        }
                    }
                }
            }).ConfigureAwait(false);

            await DownloadParametroImagesAsync(parametros).ConfigureAwait(false);
        }

        private async Task DownloadParametroImagesAsync(Parametros parametros)
        {
            var parametroComImagens = parametros.parametros.Where(p => p.alternativas.Any(pa => !string.IsNullOrEmpty(pa.urlImagem)));
            var totalImages = parametroComImagens.Sum(p => p.alternativas.Count(a => !string.IsNullOrEmpty(a.urlImagem)));
            if (totalImages == 0) return;

            var imagens = parametroComImagens
                .SelectMany(p => p.alternativas
                    .Where(a => !string.IsNullOrEmpty(a.urlImagem))
                    .Select(a => new { Parametro = p, Alternativa = a }))
                .ToList();

            var token = _cts?.Token ?? CancellationToken.None;
            int concluido = 0;

            ReportSubTask(
                mainText: Traducao.BaixandoParâmetrosDaNuvem,
                subText: Traducao.BaixandoImagensDosParâmetrosDaNuvem,

                subAtual: 0,
                subTotal: totalImages);

            await Parallel.ForEachAsync(imagens, new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = token }, async (item, ct) =>
            {
                await _imagesSemaphore.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    var seq = Interlocked.Increment(ref downloadNuvem);
                    var normalizedImageName = ParametroAlternativasFromWebService.NormalizeImageFileName(item.Alternativa.urlImagem);
                    var expectedLocalPath = ParametroAlternativasFromWebService.BuildLocalImagePath(item.Alternativa.urlImagem);

                    Interlocked.Increment(ref _pendingDownloads);
                    try
                    {
                        Debug.WriteLine($"[Sync.ParametroImagem] ↓ Download start seq={seq} alt={item.Alternativa.id} raw='{item.Alternativa.urlImagem}' normalized='{normalizedImageName}' expected='{expectedLocalPath}'");

                        var success = await _webService.DownloadImage(
                            seq,
                            item.Alternativa.urlImagem,
                            null,
                            ct,
                            normalizedImageName).ConfigureAwait(false);

                        var exists = !string.IsNullOrEmpty(expectedLocalPath) && File.Exists(expectedLocalPath);
                        var size = exists ? new FileInfo(expectedLocalPath).Length : 0;

                        Debug.WriteLine($"[Sync.ParametroImagem] ↑ Download end seq={seq} success={success} exists={exists} size={size} path='{expectedLocalPath}'");

                        if (!success)
                            Debug.WriteLine($"[Sync.ParametroImagem] Falha ao baixar imagem (ignorado): {item.Alternativa.urlImagem}");
                    }
                    finally { Interlocked.Decrement(ref _pendingDownloads); }

                    var done = Interlocked.Increment(ref concluido);

                    // ✅ Atualiza a cada item ou 5%
                    if (done % Math.Max(1, totalImages / 20) == 0 || done == totalImages)
                    {
                        ReportSubTask(
                            mainText: Traducao.BaixandoParâmetrosDaNuvem,
                            subText: Traducao.BaixandoImagensDosParâmetrosDaNuvem,
            
                            subAtual: done,
                            subTotal: totalImages,
                            detail: $"Imagem {done}/{totalImages}");
                    }
                }
                finally
                {
                    _imagesSemaphore.Release();
                }
            });
        }

        private async Task SyncModelosIsiMacroAsync(string jsonParameters)
        {
            using var requestBody = new StringContent(EncryptDecrypt.Encrypt(jsonParameters));
            var result = await _webService.ExecutePostAndWaitResult(requestBody, "getModeloIsiMacro").ConfigureAwait(false);
            if (!result.sucesso)
                throw new Exception($"Erro ao baixar Modelos ISI Macro: {result.mensagem}");
            var modeloIsiMacroFromWeb = JsonConvert.DeserializeObject<ListaModelosIsiMacroFromWeb>(result.data);
            if (modeloIsiMacroFromWeb?.ModelosIsiMacro == null) return;

            await Db.RunInTransactionAsync(conn =>
            {
                foreach (var modeloIsiMacro in modeloIsiMacroFromWeb.ModelosIsiMacro)
                {
                    conn.InsertOrReplace(new ModeloIsiMacro { Id = modeloIsiMacro.Id, NomeModelo = modeloIsiMacro.NomeModelo });
                    var parametrosIds = string.Join(",", modeloIsiMacro.Parametros.Select(p => p.id));
                    conn.Execute($"delete from ModeloIsiMacroParametro where ModeloIsiMacroId={modeloIsiMacro.Id} and ParametroId not in ({parametrosIds})");
                    foreach (var parametro in modeloIsiMacro.Parametros)
                        conn.InsertOrReplace(new ModeloIsiMacroParametro { ModeloIsiMacroId = modeloIsiMacro.Id, ParametroId = parametro.id });
                }
            }).ConfigureAwait(false);
        }

        private async Task SyncProprietariosAsync(string jsonParameters)
        {
            using var requestBody = new StringContent(EncryptDecrypt.Encrypt(jsonParameters));
            var result = await _webService.ExecutePostAndWaitResult(requestBody, "getProprietarios").ConfigureAwait(false);
            if (!result.sucesso)
                throw new Exception($"Erro ao baixar Proprietários: {result.data}");
            var proprietarios = JsonConvert.DeserializeObject<ProprietariosFromWebService>(result.data);
            if (proprietarios?.proprietarios == null) return;

            await Db.RunInTransactionAsync(conn =>
            {
                foreach (var p in proprietarios.proprietarios)
                    conn.InsertOrReplace(new Proprietario { id = p.id, nome = p.nome, dataUltimaAtualizacao = p.dataUltimaAtualizacao, excluido = p.excluido, status = p.status });
            }).ConfigureAwait(false);
        }

        private async Task SyncRegionaisAsync(string jsonParameters)
        {
            using var requestBody = new StringContent(EncryptDecrypt.Encrypt(jsonParameters));
            var result = await _webService.ExecutePostAndWaitResult(requestBody, "getRegionais").ConfigureAwait(false);
            if (!result.sucesso)
                throw new Exception($"Erro ao baixar Regionais: {result.data}");
            var regionais = JsonConvert.DeserializeObject<RegionaisFromWebService>(result.data);
            if (regionais?.regionais == null) return;

            await Db.RunInTransactionAsync(conn =>
            {
                foreach (var r in regionais.regionais)
                    conn.InsertOrReplace(new Regional { id = r.id, idApp = r.idApp ?? r.id, nome = r.nome, dataUltimaAtualizacao = r.dataUltimaAtualizacao, excluido = r.excluido, status = r.status ?? 0 });
            }).ConfigureAwait(false);
        }

        private async Task SyncPropriedadesAsync(string jsonParameters)
        {
            using var requestBody = new StringContent(EncryptDecrypt.Encrypt(jsonParameters));
            var result = await _webService.ExecutePostAndWaitResult(requestBody, "getPropriedades").ConfigureAwait(false);
            if (!result.sucesso)
                throw new Exception($"Erro ao baixar Propriedades: {result.data}");
            // Corrige typo histórico na resposta do servidor ("proriedades" → "propriedades").
            result.data = result.data.Replace("proriedades", "propriedades");
            var propriedades = JsonConvert.DeserializeObject<PropriedadesFromWebService>(result.data);
            if (propriedades?.propriedades == null) return;

            await Db.RunInTransactionAsync(conn =>
            {
                foreach (var propriedade in propriedades.propriedades)
                {
                    conn.InsertOrReplace(new Propriedade
                    {
                        id = propriedade.id,
                        idApp = propriedade.idApp ?? propriedade.id,
                        nome = propriedade.nome,
                        proprietarioId = propriedade.proprietarioId,
                        regionalId = propriedade.regionalId,
                        dataUltimaAtualizacao = propriedade.dataUltimaAtualizacao,
                        excluido = propriedade.excluido,
                        status = propriedade.status
                    });
                    foreach (var parametro in propriedade.parametros)
                        conn.InsertOrReplace(new PropriedadeParametro { parametroId = parametro.parametroId, propriedadeId = propriedade.id, valor = parametro.valor });
                }
            }).ConfigureAwait(false);
        }

        private async Task SyncUnidadesEpidemiologicasAsync(string jsonParameters)
        {
            using var requestBody = new StringContent(EncryptDecrypt.Encrypt(jsonParameters));
            var result = await _webService.ExecutePostAndWaitResult(requestBody, "getUnidadesEpidemiologicas").ConfigureAwait(false);
            if (!result.sucesso)
                throw new Exception($"Erro ao baixar Unidades Epidemiológicas: {result.data}");
            var unidades = JsonConvert.DeserializeObject<UnidadesEpidemiolgicasFromWebService>(result.data);
            if (unidades?.unidadesEpidemiologicas == null) return;

            await Db.RunInTransactionAsync(conn =>
            {
                foreach (var u in unidades.unidadesEpidemiologicas)
                {
                    conn.InsertOrReplace(new UnidadeEpidemiologica
                    {
                        id = u.id,
                        idApp = u.idApp ?? u.id,
                        nome = u.nome,
                        propriedadeId = u.propriedadeId,
                        dataUltimaAtualizacao = u.dataUltimaAtualizacao,
                        excluido = u.excluido,
                        status = u.status,
                        latitude = u.latitude,
                        longitude = u.longitude
                    });
                    foreach (var parametro in u.parametros)
                        conn.InsertOrReplace(new UnidadeEpidemiologicaParametro { parametroId = parametro.parametroId, unidadeEpidemiologicaId = u.id, valor = parametro.valor });
                }
            }).ConfigureAwait(false);
        }

        private async Task SyncTipoCorAtividadesAsync(string jsonParameters)
        {
            using var requestBody = new StringContent(EncryptDecrypt.Encrypt(jsonParameters));
            var result = await _webService.ExecutePostAndWaitResult(requestBody, "getTipoCorAtividades").ConfigureAwait(false);
            if (!result.sucesso)
                throw new Exception($"Erro ao baixar Tipo Cor Atividades: {result.data}");
            var tipoCor = JsonConvert.DeserializeObject<TipoCorAtividadesFromWebService>(result.data);
            if (tipoCor?.TipoCorAtividades == null) return;

            await Db.RunInTransactionAsync(conn =>
            {
                foreach (var t in tipoCor.TipoCorAtividades)
                    conn.InsertOrReplace(new TipoCorAtividade { id = t.id, nome = t.nome });
            }).ConfigureAwait(false);
        }

        private async Task SyncTipoAtividadesAsync(string jsonParameters)
        {
            using var requestBody = new StringContent(EncryptDecrypt.Encrypt(jsonParameters));
            var result = await _webService.ExecutePostAndWaitResult(requestBody, "getTipoAtividades").ConfigureAwait(false);
            if (!result.sucesso)
                throw new Exception($"Erro ao baixar Tipo Atividades: {result.data}");
            var tipoAtividades = JsonConvert.DeserializeObject<TipoAtividadesFromWebService>(result.data);
            if (tipoAtividades?.TipoAtividades == null) return;

            await Db.RunInTransactionAsync(conn =>
            {
                foreach (var t in tipoAtividades.TipoAtividades)
                    conn.InsertOrReplace(new TipoAtividade
                    {
                        id = t.id,
                        nome = t.nome,
                        status = t.status,
                        atividadeTipoCorId = t.atividadeTipoCorId,
                        dataUltimaAtualizacao = t.dataUltimaAtualizacao,
                        excluido = t.excluido
                    });
            }).ConfigureAwait(false);
        }

        // Lotes fechados não são inseridos — apenas os que foram encerrados desde a última
        // sincronização são removidos do dispositivo. Preserva formulários com pendência local.
        private async Task SyncLotesFechadosAsync(getDataDefaultParameters parameters)
        {
            var ultimaAtualizacao = Preferences.Get("lastsyncdatetime", DateTime.MinValue);
            var loteparameters = new getLotesParameters
            {
                usuario = parameters.usuario,
                dispositivoId = parameters.dispositivoId,
                session = parameters.session,
                status = 2,
                idioma = parameters.idioma,
                data = ultimaAtualizacao != DateTime.MinValue ? ultimaAtualizacao.ToString("yyyy-MM-dd") : ""
            };
            var json = JsonConvert.SerializeObject(loteparameters).Replace("\"data\":\"\",", "");
            using var body = new StringContent(EncryptDecrypt.Encrypt(json));
            var result = await _webService.ExecutePostAndWaitResult(body, "getLotes").ConfigureAwait(false);
            if (!result.sucesso)
                throw new Exception($"Erro ao baixar Lotes Fechados: {result.data}");
            var lotesWeb = JsonConvert.DeserializeObject<Lote.LotesFromWebService>(result.data);
            if (lotesWeb?.lotes == null) return;

            await Db.RunInTransactionAsync(conn =>
            {
                foreach (var l in lotesWeb.lotes.Where(x => x.loteStatus == 2))
                {
                    var pendingFormsCount = conn.ExecuteScalar<int>($"SELECT COUNT(*) FROM LoteForm WHERE (loteId={(l.idApp ?? l.id)}) AND temmudanca=1");
                    if (pendingFormsCount == 0)
                        conn.Execute(l.idApp != null ? $"delete from lote where idApp={l.idApp}" : $"delete from lote where id={l.id}");
                }
            }).ConfigureAwait(false);
        }

        private async Task SyncLotesAbertosAsync(getDataDefaultParameters parameters)
        {
            var ultimaAtualizacao = Preferences.Get("lastsyncdatetime", DateTime.MinValue);
            var loteparameters = new getLotesParameters
            {
                usuario = parameters.usuario,
                dispositivoId = parameters.dispositivoId,
                session = parameters.session,
                status = 1,
                idioma = parameters.idioma,
                data = ultimaAtualizacao != DateTime.MinValue ? ultimaAtualizacao.ToString("yyyy-MM-dd") : ""
            };
            var json = JsonConvert.SerializeObject(loteparameters).Replace("\"data\":\"\",", "");
            using var body = new StringContent(EncryptDecrypt.Encrypt(json));
            var result = await _webService.ExecutePostAndWaitResult(body, "getLotes").ConfigureAwait(false);
            if (!result.sucesso)
                throw new Exception($"Erro ao baixar Lotes Abertos: {result.data}");
            var lotesWeb = JsonConvert.DeserializeObject<Lote.LotesFromWebService>(result.data);
            if (lotesWeb?.lotes == null) return;

            await Db.RunInTransactionAsync(conn =>
            {
                foreach (var l in lotesWeb.lotes.Where(x => x.loteStatus != 2 && x.excluido != 1))
                {
                    // Preserva ISIMacroScoreMedio calculado localmente — o servidor não envia esse campo
                    // e InsertOrReplace zeraria o score calculado antes de AtualizaTodasMediasISIMacroComTransacao rodar
                    var scoreExistente = conn.Find<Lote>(l.id)?.ISIMacroScoreMedio ?? 0;

                    conn.InsertOrReplace(new Lote
                    {
                        id = l.id,
                        idApp = l.idApp ?? l.id,
                        dataUltimaAtualizacao = l.dataUltimaAtualizacao,
                        excluido = l.excluido,
                        conversaoAlimentarReal = l.conversaoAlimentarReal ?? 0f,
                        dataAbate = l.dataAbate ?? DateTime.MinValue,
                        dataInicio = l.dataInicio ?? DateTime.MinValue,
                        mortalidade = l.mortalidade ?? 0f,
                        numero = l.numero,
                        pesoInicial = l.pesoInicial ?? 0,
                        pesoFinal = l.pesoFinal ?? 0f,
                        loteStatus = l.loteStatus,
                        unidadeEpidemiologicaId = l.unidadeEpidemiologicaId,
                        ISIMacroScoreMedio = scoreExistente
                    });
                    foreach (var parametro in l.parametros)
                        conn.InsertOrReplace(new LoteParametro { parametroId = parametro.parametroId, loteId = l.id, valor = parametro.valor });
                }
            }).ConfigureAwait(false);

        }

        private async Task SyncAtividadesAsync(string jsonParameters)
        {
            using var requestBody = new StringContent(EncryptDecrypt.Encrypt(jsonParameters));
            var result = await _webService.ExecutePostAndWaitResult(requestBody, "getAtividades").ConfigureAwait(false);
            if (!result.sucesso)
                throw new Exception($"Erro ao baixar Atividades: {result.data}");
            var atividades = JsonConvert.DeserializeObject<AtividadesFromWebService>(result.data);
            if (atividades?.Atividades == null) return;

            await Db.RunInTransactionAsync(conn =>
            {
                foreach (var a in atividades.Atividades)
                {
                    conn.InsertOrReplace(new Atividade
                    {
                        id = a.id,
                        idApp = a.idApp ?? a.id,
                        atividadeStatus = a.atividadeStatus,
                        atividadeTipoData = a.atividadeTipoData,
                        atividadeTipoId = a.atividadeTipoId,
                        dataHoraInicio = a.dataHoraInicio,
                        dataHoraPrazo = a.dataHoraPrazo,
                        dataInicio = a.dataHoraInicio?.Date ?? DateTime.MinValue,
                        dataPrazo = a.dataHoraPrazo?.Date ?? DateTime.MinValue,
                        horaInicio = a.dataHoraInicio?.TimeOfDay ?? TimeSpan.MinValue,
                        horaPrazo = a.dataHoraPrazo?.TimeOfDay ?? TimeSpan.MinValue,
                        dataUltimaAtualizacao = a.dataUltimaAtualizacao,
                        descricao = a.descricao,
                        titulo = a.titulo,
                        excluido = a.excluido,
                        unidadeEpidemiologicaId = a.unidadeEpidemiologicaId,
                        usuarioResponsavelId = a.usuarioResponsavelId
                    });
                    foreach (var ou in a.outrosUsuarios)
                        conn.InsertOrReplace(new AtividadeOutroUsuario { atividadeId = a.id, usuarioId = ou.usuarioId });
                }
            }).ConfigureAwait(false);
        }

        private async Task SyncNotificacoesAsync(string jsonParameters)
        {
            using var requestBody = new StringContent(EncryptDecrypt.Encrypt(jsonParameters));
            var result = await _webService.ExecutePostAndWaitResult(requestBody, "getNotificacoes").ConfigureAwait(false);
            if (!result.sucesso)
                throw new Exception($"Erro ao baixar Notificações: {result.data}");
            var notificacoes = JsonConvert.DeserializeObject<NotificacaoListFromWebService>(result.data);
            if (notificacoes?.notificacoes == null) return;

            await Db.RunInTransactionAsync(conn =>
            {
                foreach (var n in notificacoes.notificacoes)
                    conn.InsertOrReplace(new Notificacao { id = n.id, idApp = n.idApp > 0 ? n.idApp : n.id, descricao = n.descricao, titulo = n.titulo, dataHora = n.dataHora });
            }).ConfigureAwait(false);
        }

        private async Task SyncPermissoesAsync(string jsonParameters)
        {
            using var requestBody = new StringContent(EncryptDecrypt.Encrypt(jsonParameters));
            var result = await _webService.ExecutePostAndWaitResult(requestBody, "getPermissoes").ConfigureAwait(false);
            if (!result.sucesso)
                throw new Exception($"Erro ao baixar Permissões: {result.data}");
            if (string.IsNullOrWhiteSpace(result.data)) return;

            try
            {
                using var jsonDocument = JsonDocument.Parse(result.data);
                var root = jsonDocument.RootElement;
                if (!root.TryGetProperty("permissoes", out var permissoesElement))
                    return;

                var jsonPermissoes = permissoesElement.GetRawText();
#if DEBUG
                Debug.WriteLine($"[Sync] JSON de permissoes bruto: {jsonPermissoes}");
#endif
                var permissoes = JsonConvert.DeserializeObject<Permissoes>(jsonPermissoes);
                if (permissoes == null)
                    return;

                if (root.TryGetProperty("sistemaTermos", out var sistemaTermosElement))
                {
                    var jsonSistemaTermos = sistemaTermosElement.GetRawText();
                    try
                    {
                        permissoes.SistemaTermos = JsonConvert.DeserializeObject<SistemaTermos>(jsonSistemaTermos);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Sync] Falha ao desserializar SistemaTermos: {ex.Message}");
                    }
                }

                Permissoes.UsuarioPermissoes = permissoes;
                Permissoes.NotifyAllStaticPropertiesChanged();
                try { Preferences.Set("Permissoes", JsonConvert.SerializeObject(Permissoes.UsuarioPermissoes)); }
                catch (Exception ex) { Debug.WriteLine($"[Sync] Falha ao salvar permissões: {ex.Message}"); }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Sync] JSON inválido em getPermissoes: {ex.Message}");
                throw;
            }
        }

        public async Task AtualizaTodasMediasISIMacroComTransacao()
        {
            var stopwatch = Stopwatch.StartNew();
            await Db.ExecuteAsync("DROP TABLE IF EXISTS temp.tmp_form_scores");
            await Db.ExecuteAsync("CREATE TEMP TABLE IF NOT EXISTS tmp_form_scores (LoteId INT NOT NULL, ISIMacroScore REAL)");

            var insertSql = @"
                INSERT INTO tmp_form_scores (LoteId, ISIMacroScore)
                SELECT 
                    lf.loteId,
                    SUM(pa.score * p.peso) AS ISIMacroScore
                FROM LoteFormParametro lfp
                INNER JOIN LoteForm lf ON lf.id = lfp.LoteFormId
                INNER JOIN Parametro p ON p.id = lfp.parametroId AND p.Tipo = 0  -- Tipo = 0: exclui parâmetros Isolados do cálculo de score médio do lote
                INNER JOIN ParametroAlternativas pa ON pa.idParametro = p.id AND pa.id = lfp.valor
                WHERE lf.parametroTipoId = 15
                GROUP BY lf.loteId, lfp.LoteFormId;";
            await Db.ExecuteAsync(insertSql);

            var updateSql = @"
                UPDATE Lote AS l
                SET ISIMacroScoreMedio = (
                    SELECT AVG(t.ISIMacroScore)
                    FROM tmp_form_scores t
                    WHERE t.LoteId = l.Id
                )
                WHERE EXISTS (SELECT 1 FROM tmp_form_scores t2 WHERE t2.LoteId = l.Id);";
            await Db.ExecuteAsync(updateSql);
            await Db.ExecuteAsync("DROP TABLE IF EXISTS temp.tmp_form_scores");

            Debug.WriteLine($"[Lote] ⚡ AtualizaTodasMediasISIMacroComTransacao: {stopwatch.ElapsedMilliseconds}ms");
            Preferences.Set("UltimaAtualizacaoTodasMediasISIMacro", DateTime.Now);
        }

        // === Formulários de Lote ===

        private async Task BaixaFormulariosLoteDaWeb(getDataDefaultParameters parameters)
        {
            var lotes = await Lote.PegaListaLotesAsync().ConfigureAwait(false);
            var total = lotes.Count;
            // Reseta o contador de downloads antes de cada bloco paralelo — garante que
            // o progresso reporte de 0 a N mesmo que outro bloco já tenha usado o campo.
            Interlocked.Exchange(ref downloadNuvem, 0);

            ReportSubTask(
                mainText: Traducao.BaixandoFormuláriosDosLotesDaNuvem,
                subText: Traducao.BaixandoFormulários,

                subAtual: 0,
                subTotal: total);

            var results = new System.Collections.Concurrent.ConcurrentBag<LoteResultado>();
            var token = _cts?.Token ?? CancellationToken.None;
            await Parallel.ForEachAsync(lotes, new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = token }, async (lote, ct) =>
            {
                var result = await DownloadLoteFormWithProgressAsync(lote, parameters, total, ct).ConfigureAwait(false);
                results.Add(result);
            });
            await ProcessarResultadosEmBatch(results.ToArray(), total).ConfigureAwait(false);
        }

        private async Task<LoteResultado> DownloadLoteFormWithProgressAsync(Lote lote, getDataDefaultParameters parameters, int total, CancellationToken ct = default)
        {
            var getLoteFormParameters = new getLoteFormsParameters(parameters) { lote = lote.id };
            var json = JsonConvert.SerializeObject(getLoteFormParameters);
            var encrypted = EncryptDecrypt.Encrypt(json);

            using var body = new StringContent(encrypted);
            var resultado = await _webService.ExecutePostAndWaitResult(body, "getLoteFormsSincronizar").ConfigureAwait(false);
            var done = Interlocked.Increment(ref downloadNuvem);

            if (done % Math.Max(1, total / 20) == 0 || done == total)
            {
                ReportSubTask(
                    mainText: Traducao.BaixandoFormuláriosDosLotesDaNuvem,
                    subText: Traducao.BaixandoFormulários,
    
                    subAtual: done,
                    subTotal: total,
                    detail: $"Lote {lote.id}");
            }

            return new LoteResultado { LoteId = lote.id, Resultado = resultado };
        }

        private async Task ProcessarResultadosEmBatch(LoteResultado[] results, int total)
        {
            var lotesFormsParaProcessar = new List<(int loteId, LotesFormFromWebService loteForms)>();
            foreach (var result in results)
            {
                if (result.Resultado.sucesso && !string.IsNullOrEmpty(result.Resultado.data))
                {
                    try
                    {
                        var lotesforms = JsonConvert.DeserializeObject<LotesFormFromWebService>(result.Resultado.data);
                        if (lotesforms?.loteForms?.Count > 0)
                            lotesFormsParaProcessar.Add(((int)result.LoteId, lotesforms));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Sync] Erro ao desserializar formulários do lote {result.LoteId}: {ex.Message}");
                        SentryHelper.CaptureExceptionWithUser(ex, null, $"ProcessarResultadosEmBatch lote={result.LoteId}");
                    }
                }
            }

            int loteProcessado = 0;
            foreach (var (loteId, loteForms) in lotesFormsParaProcessar)
            {
                loteProcessado++;
                if (loteProcessado % Math.Max(1, results.Length / 20) == 0 || loteProcessado == results.Length)
                {
                    ReportSubTask(
                        mainText: Traducao.BaixandoFormuláriosDosLotesDaNuvem,
                        subText: Traducao.ProcessandoFormuláriosBaixadosDaNuvem,
        
                        subAtual: loteProcessado,
                        subTotal: results.Length,
                        detail: $"{loteProcessado}/{results.Length}");
                }
            }

            await DeletarFormulariosAntigosEmBatch(lotesFormsParaProcessar).ConfigureAwait(false);
            await InserirFormulariosEmBatch(lotesFormsParaProcessar, total).ConfigureAwait(false);
        }

        private async Task DeletarFormulariosAntigosEmBatch(List<(int loteId, LotesFormFromWebService loteForms)> lotesFormsParaProcessar)
        {
            await Db.RunInTransactionAsync(conn =>
            {
                foreach (var (loteId, loteForms) in lotesFormsParaProcessar)
                {
                    var ids = string.Join(",", loteForms.loteForms.Select(lf => lf.id));
                    if (!string.IsNullOrEmpty(ids))
                    {
                        conn.Execute($"DELETE FROM LoteFormParametro WHERE LoteFormId IN ({ids})");
                        conn.Execute($"DELETE FROM LoteFormImagem WHERE LoteFormId IN ({ids})");
                        conn.Execute($"DELETE FROM LoteForm WHERE id IN ({ids})");
                    }
                }
            }).ConfigureAwait(false);
        }

        private async Task InserirFormulariosEmBatch(List<(int loteId, LotesFormFromWebService loteForms)> lotesFormsParaProcessar, int total)
        {
            var loteFormsDaWebProcessados = new List<LoteFormFromWebService>();
            var totalImagens = 0;
            var loteProcessado = 0;

            await Db.RunInTransactionAsync(conn =>
            {
                foreach (var (loteId, loteForms) in lotesFormsParaProcessar)
                {
                    loteProcessado++;
                    var lotesFormsParaInserir = new List<LoteForm>();
                    var parametrosParaInserir = new List<LoteFormParametro>();

                    foreach (var loteform in loteForms.loteForms.Where(lf => lf.excluido != 1))
                    {
                        loteFormsDaWebProcessados.Add(loteform);

                        lotesFormsParaInserir.Add(new LoteForm
                        {
                            id = loteform.id,
                            idApp = loteform.idApp ?? loteform.id,
                            dataUltimaAtualizacao = loteform.dataUltimaAtualizacao,
                            data = loteform.data,
                            loteFormVinculado = loteform.loteFormVinculado,
                            observacoes = loteform.observacoes,
                            loteId = loteform.loteId,
                            loteFormFase = loteform.loteFormFaseId,
                            loteVisita = loteform.loteVisita,
                            parametroTipoId = loteform.parametroTipoId,
                            item = loteform.item
                        });

                        if (loteform.parametros?.Count > 0)
                        {
                            parametrosParaInserir.AddRange(loteform.parametros.Select(parametro => new LoteFormParametro
                            {
                                parametroId = parametro.parametroId,
                                LoteFormId = loteform.id,
                                valor = parametro.valor
                            }));
                        }
                        totalImagens += loteform.imagens?.Count ?? 0;
                    }

                    if (lotesFormsParaInserir.Any()) conn.InsertAll(lotesFormsParaInserir);
                    if (parametrosParaInserir.Any()) conn.InsertAll(parametrosParaInserir);
                }
            }).ConfigureAwait(false);

            if (totalImagens > 0)
            {
                await DownloadLoteFormImagesAsync(loteFormsDaWebProcessados, totalImagens).ConfigureAwait(false);
            }
        }

        // === Avaliação Galpão ===

        private async Task BaixaFormulariosAvaliacoesGalpaoDaWeb(getDataDefaultParameters parameters)
        {
            var lotes = await Lote.PegaListaLotesAsync().ConfigureAwait(false);
            if (lotes == null || lotes.Count == 0) return;

            var resultados = new System.Collections.Concurrent.ConcurrentBag<LoteResultado>();
            Interlocked.Exchange(ref downloadNuvem, 0);
            var total = lotes.Count;
            var token = _cts?.Token ?? CancellationToken.None;

            await Parallel.ForEachAsync(lotes, new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = token }, async (lote, ct) =>
            {
                var getParams = new getLoteFormsParameters(parameters) { lote = lote.id };
                var json = JsonConvert.SerializeObject(getParams);
                var encrypted = EncryptDecrypt.Encrypt(json);
                using var body = new StringContent(encrypted);
                var resultado = await _webService.ExecutePostAndWaitResult(body, "getLoteFormsGalpao").ConfigureAwait(false);
                var done = Interlocked.Increment(ref downloadNuvem);

                if (done % Math.Max(1, total / 20) == 0 || done == total)
                {
                    ReportSubTask(
                        mainText: Traducao.BaixandoAvaliaçõesDoGalpãoDaNuvem,
                        subText: Traducao.BaixandoAvaliacaoes,
        
                        subAtual: done,
                        subTotal: total,
                        detail: $"Lote {lote.id}");
                }

                resultados.Add(new LoteResultado { LoteId = lote.id, Resultado = resultado });
            }).ConfigureAwait(false);

            await ProcessarAvaliacoesGalpao(resultados).ConfigureAwait(false);
        }

        private async Task ProcessarAvaliacoesGalpao(IEnumerable<LoteResultado> resultados)
        {
            ReportSubTask(Traducao.ProcessandoAvaliacaoesGalpaoNuvem, "", 0, null, incrementar: false);

            var avaliacoesGalpaoDaWebProcessados = new System.Collections.Concurrent.ConcurrentBag<LoteFormGalpaoWebService>();
            Parallel.ForEach(resultados, r =>
            {
                if (!r.Resultado.sucesso || string.IsNullOrEmpty(r.Resultado.data)) return;
                try
                {
                    var parsed = JsonConvert.DeserializeObject<ResultadogetLoteFormsGalpao>(r.Resultado.data);
                    if (parsed?.loteForms != null)
                    {
                        foreach (var lf in parsed.loteForms.Where(lf => lf.parametrosGalpao?.Count > 0))
                            avaliacoesGalpaoDaWebProcessados.Add(lf);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Sync] Erro ao desserializar avaliação galpão lote {r.LoteId}: {ex.Message}");
                    SentryHelper.CaptureExceptionWithUser(ex, null, $"ProcessarAvaliacoesGalpao lote={r.LoteId}");
                }
            });

            if (!avaliacoesGalpaoDaWebProcessados.Any()) return;

            var idsParaExcluir = avaliacoesGalpaoDaWebProcessados
                .Where(lf => lf.id.HasValue)
                .Select(lf => lf.id!.Value)
                .Distinct()
                .ToList();

            if (idsParaExcluir.Any())
            {
                var idsString = string.Join(",", idsParaExcluir);
                await Db.RunInTransactionAsync(conn =>
                {
                    conn.Execute($"DELETE FROM LoteFormParametro WHERE LoteFormId IN ({idsString})");
                    conn.Execute($"DELETE FROM LoteForm WHERE id IN ({idsString})");
                    conn.Execute($"DELETE FROM LoteFormImagem WHERE LoteFormId IN ({idsString})");
                    conn.Execute($"DELETE FROM LoteFormAvaliacaoGalpao WHERE LoteFormId IN ({idsString})");
                }).ConfigureAwait(false);
            }

            var todasAvaliacoes = new List<LoteForm>();
            var todasAvaliacoesGalpao = new List<LoteFormAvaliacaoGalpao>();
            var totalImagensGalpao = 0;

            foreach (var resultadoloteForm in avaliacoesGalpaoDaWebProcessados)
            {
                todasAvaliacoes.Add(new LoteForm
                {
                    id = resultadoloteForm.id,
                    dataUltimaAtualizacao = resultadoloteForm.dataUltimaAtualizacao,
                    data = resultadoloteForm.data,
                    observacoes = resultadoloteForm.observacoes,
                    loteId = resultadoloteForm.loteId,
                    loteFormFase = resultadoloteForm.loteFormFaseId,
                    loteVisita = resultadoloteForm.loteVisita,
                    parametroTipoId = resultadoloteForm.parametroTipoId,
                    item = resultadoloteForm.item
                });

                if (resultadoloteForm.parametrosGalpao != null && resultadoloteForm.parametros?.Count > 0)
                {
                    var parametroID = resultadoloteForm.parametros.First().parametroId;
                    var agregados = new Dictionary<int, LoteFormAvaliacaoGalpao>();

                    foreach (var parametro in resultadoloteForm.parametrosGalpao)
                    {
                        if (string.IsNullOrEmpty(parametro.parametroAlternativa))
                        {
                            // Quantitativo (sem alternativa)
                            agregados[parametro.item ?? parametro.indice ?? 0] = new LoteFormAvaliacaoGalpao
                            {
                                parametroId = parametroID,
                                LoteFormId = resultadoloteForm.id,
                                NumeroResposta = parametro.item ?? parametro.indice,
                                RespostaQtde = parametro.valor
                            };
                        }
                        else
                        {
                            // Qualitativo (com alternativa)
                            var numero = parametro.indice ?? parametro.item ?? 0;
                            if (!agregados.TryGetValue(numero, out var existente))
                            {
                                existente = new LoteFormAvaliacaoGalpao
                                {
                                    parametroId = parametroID,
                                    LoteFormId = resultadoloteForm.id,
                                    NumeroResposta = numero,
                                    RespostaQtde = null
                                };
                                agregados[numero] = existente;
                            }

                            if (int.TryParse(parametro.parametroAlternativa, out var altId))
                            {
                                var lista = existente.AlternativaIds;
                                if (!lista.Contains(altId))
                                {
                                    lista.Add(altId);
                                    existente.AlternativaIds = lista;
                                }
                            }
                        }
                    }

                    todasAvaliacoesGalpao.AddRange(agregados.Values);
                }

                totalImagensGalpao += resultadoloteForm.imagens?.Count ?? 0;
            }

            await Db.RunInTransactionAsync(conn =>
            {
                if (todasAvaliacoes.Any()) conn.InsertAll(todasAvaliacoes);
                if (todasAvaliacoesGalpao.Any()) conn.InsertAll(todasAvaliacoesGalpao);
            }).ConfigureAwait(false);

            if (totalImagensGalpao > 0)
                await DownloadAvaliacaoGalpaoImagesAsync(avaliacoesGalpaoDaWebProcessados, totalImagensGalpao).ConfigureAwait(false);
        }

        private async Task DownloadLoteFormImagesAsync(List<LoteFormFromWebService> loteFormsDaWeb, int totalImagens)
        {
            if (loteFormsDaWeb == null || totalImagens <= 0) return;

            var token = _cts?.Token ?? CancellationToken.None;
            int concluido = 0;

            ReportSubTask(
                mainText: Traducao.BaixandoFormuláriosDosLotesDaNuvem,
                subText: Traducao.BaixandoImagensDosFormuláriosDaNuvem,

                subAtual: 0,
                subTotal: totalImagens);

            var imagens = loteFormsDaWeb
                .Where(lf => lf.imagens != null && lf.imagens.Count > 0)
                .SelectMany(lf => lf.imagens.Select(img => new { Form = lf, Img = img }))
                .ToList();

            await Parallel.ForEachAsync(imagens, new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = token }, async (item, ct) =>
            {
                await _imagesSemaphore.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    var seq = Interlocked.Increment(ref downloadNuvem);
                    var fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{seq}.jpg";
                    Interlocked.Increment(ref _pendingDownloads);
                    try
                    {
                        await _webService.DownloadImage(seq, $"https://webapp.isiinstitute.com/{item.Img.url}", null, ct, fileName).ConfigureAwait(false);
                        await Db.InsertAsync(new LoteFormImagem { LoteFormId = item.Form.id, url = fileName }).ConfigureAwait(false);
                    }
                    finally { Interlocked.Decrement(ref _pendingDownloads); }

                    var done = Interlocked.Increment(ref concluido);
                    if (done % Math.Max(1, totalImagens / 20) == 0 || done == totalImagens)
                    {
                        ReportSubTask(
                            mainText: Traducao.BaixandoFormuláriosDosLotesDaNuvem,
                            subText: Traducao.BaixandoImagensDosFormuláriosDaNuvem,
            
                            subAtual: done,
                            subTotal: totalImagens,
                            detail: $"{done}/{totalImagens}");
                    }
                }
                finally { _imagesSemaphore.Release(); }
            });
        }

        private async Task DownloadAvaliacaoGalpaoImagesAsync(IEnumerable<LoteFormGalpaoWebService> avaliacoesGalpao, int totalImagens)
        {
            if (avaliacoesGalpao == null || totalImagens <= 0) return;

            var token = _cts?.Token ?? CancellationToken.None;
            int concluido = 0;

            ReportSubTask(
                mainText: Traducao.BaixandoAvaliaçõesDoGalpãoDaNuvem,
                subText: Traducao.BaixandoImagensDosFormuláriosDaNuvem,

                subAtual: 0,
                subTotal: totalImagens);

            var imagens = avaliacoesGalpao
                .Where(lf => lf.imagens != null && lf.imagens.Count > 0)
                .SelectMany(lf => lf.imagens.Select(img => new { Form = lf, Img = img }))
                .ToList();

            await Parallel.ForEachAsync(imagens, new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = token }, async (item, ct) =>
            {
                await _imagesSemaphore.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    var seq = Interlocked.Increment(ref downloadNuvem);
                    var fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{seq}.jpg";
                    Interlocked.Increment(ref _pendingDownloads);
                    try
                    {
                        await _webService.DownloadImage(seq, $"https://webapp.isiinstitute.com/{item.Img.url}", null, ct, fileName).ConfigureAwait(false);
                        await Db.InsertAsync(new LoteFormImagem { LoteFormId = item.Form.id, url = fileName }).ConfigureAwait(false);
                    }
                    finally { Interlocked.Decrement(ref _pendingDownloads); }

                    var done = Interlocked.Increment(ref concluido);
                    if (done % Math.Max(1, totalImagens / 20) == 0 || done == totalImagens)
                    {
                        ReportSubTask(
                            mainText: Traducao.BaixandoAvaliaçõesDoGalpãoDaNuvem,
                            subText: Traducao.BaixandoImagensDosFormuláriosDaNuvem,
            
                            subAtual: done,
                            subTotal: totalImagens,
                            detail: $"{done}/{totalImagens}");
                    }
                }
                finally { _imagesSemaphore.Release(); }
            });
        }

        #endregion
    }
}
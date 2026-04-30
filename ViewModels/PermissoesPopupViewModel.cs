using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SilvaData.Models;
using SilvaData.Utilities;

using Microsoft.Maui.Storage;

using Newtonsoft.Json;

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace SilvaData.ViewModels
{
    /// <summary>
    /// ViewModel para uma categoria de permissÃµes no popup.
    /// </summary>
    public partial class PermissaoCategoriaViewModel : ObservableObject
    {
        [ObservableProperty]
        private string nome;

        [ObservableProperty]
        private ObservableCollection<PermissionItem> permissoes = new();

        public double AlturaLista => Permissoes.Count * 35;

        public PermissaoCategoriaViewModel(string nome)
        {
            Nome = nome;
        }
    }

    /// <summary>
    /// ViewModel para o popup de permissÃµes do usuÃ¡rio (DEBUG only).
    /// </summary>
    public partial class PermissoesPopupViewModel : ObservableObject
    {
        private readonly Popup _popup;
        private bool _isClosing;

        [ObservableProperty]
        private ObservableCollection<PermissaoCategoriaViewModel> categorias = new();

        public PermissoesPopupViewModel(Popup popup)
        {
            _popup = popup;
            CarregarPermissoes();
        }

        /// <summary>
        /// Carrega todas as permissÃµes do usuÃ¡rio via reflexÃ£o.
        /// </summary>
        private void CarregarPermissoes()
        {
            Categorias.Clear();

            var permissoes = Permissoes.UsuarioPermissoes;
            if (permissoes == null) return;

            // PermissÃµes principais
            ExtrairPermissoes("Regionais", permissoes.regionais);
            ExtrairPermissoes("Propriedades", permissoes.propriedades);
            ExtrairPermissoes("ProprietÃ¡rios", permissoes.proprietarios);
            ExtrairPermissoes("Unidades EpidemiolÃ³gicas", permissoes.unidadesEpidemiologicas);
            ExtrairPermissoes("Atividades", permissoes.atividades);

            // Lotes
            ExtrairPermissoes("Lotes", permissoes.lotes);

            // Monitoramento (dentro de Lotes)
            if (permissoes.lotes?.monitoramento != null)
            {
                ExtrairPermissoes("Lotes - Sanidade", permissoes.lotes.monitoramento.sanidade);
                ExtrairPermissoes("Lotes - ZootÃ©cnico", permissoes.lotes.monitoramento.zootecnico);
                ExtrairPermissoes("Lotes - ISI Macro", permissoes.lotes.monitoramento.isiMacro);
                ExtrairPermissoes("Lotes - NutriÃ§Ã£o", permissoes.lotes.monitoramento.nutricao);
                ExtrairPermissoes("Lotes - ISI Micro", permissoes.lotes.monitoramento.isiMicro);
                ExtrairPermissoes("Lotes - Manejo", permissoes.lotes.monitoramento.manejo);
            }

            // LoteDetalhado
            var categoriaLoteDetalhado = new PermissaoCategoriaViewModel("Outros");
            var propLoteDetalhado = permissoes.GetType().GetProperty("LoteDetalhado");
            if (propLoteDetalhado != null)
            {
                var item = new PermissionItem(
                    "Lote Detalhado",
                    "Outros",
                    "LoteDetalhado",
                    permissoes.LoteDetalhado,
                    permissoes,
                    propLoteDetalhado);
                categoriaLoteDetalhado.Permissoes.Add(item);
            }

            if (categoriaLoteDetalhado.Permissoes.Count > 0)
                Categorias.Add(categoriaLoteDetalhado);
        }

        /// <summary>
        /// Extrai todas as propriedades booleanas de um objeto de permissÃµes.
        /// </summary>
        private void ExtrairPermissoes(string categoriaNome, object? permissaoObj)
        {
            if (permissaoObj == null) return;

            var categoria = new PermissaoCategoriaViewModel(categoriaNome);
            var type = permissaoObj.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                if (!prop.CanWrite) continue;
                if (prop.PropertyType != typeof(bool)) continue;

                var nomeAmigavel = FormatPermissionName(prop.Name);
                var valor = (bool)(prop.GetValue(permissaoObj) ?? false);

                var item = new PermissionItem(
                    nomeAmigavel,
                    categoriaNome,
                    prop.Name,
                    valor,
                    permissaoObj,
                    prop);

                categoria.Permissoes.Add(item);
            }

            if (categoria.Permissoes.Count > 0)
                Categorias.Add(categoria);
        }

        /// <summary>
        /// Formata o nome da propriedade para exibiÃ§Ã£o amigÃ¡vel.
        /// </summary>
        private string FormatPermissionName(string propName)
        {
            return propName switch
            {
                "consultar" => "Consultar",
                "cadastrar" => "Cadastrar",
                "atualizar" => "Atualizar",
                "excluir" => "Excluir",
                "editar" => "Editar",
                "abrir" => "Abrir",
                "fechar" => "Fechar",
                "excluirForm" => "Excluir FormulÃ¡rio",
                _ => char.ToUpper(propName[0]) + propName.Substring(1)
            };
        }

        /// <summary>
        /// Fecha o popup sem salvar.
        /// </summary>
        [RelayCommand]
        private async Task Fechar()
        {
            if (_isClosing) return; _isClosing = true; try { await _popup.CloseAsync(); } catch { }
        }

        /// <summary>
        /// Salva as permissÃµes alteradas no Preferences e notifica a UI.
        /// </summary>
        [RelayCommand]
        private async Task Salvar()
        {
            try
            {
                // Aplica as alteraÃ§Ãµes via reflexÃ£o
                foreach (var categoria in Categorias)
                {
                    foreach (var permissao in categoria.Permissoes)
                    {
                        if (permissao.Parent != null && permissao.PropertyInfo != null)
                        {
                            permissao.PropertyInfo.SetValue(permissao.Parent, permissao.Valor);
                        }
                    }
                }

                // Salva no Preferences
                var json = JsonConvert.SerializeObject(Permissoes.UsuarioPermissoes);
                Preferences.Set("Permissoes", json);

                // Notifica todas as propriedades estÃ¡ticas via mÃ©todo pÃºblico
                Permissoes.NotifyAllStaticPropertiesChanged();

                Debug.WriteLine("[PermissoesPopup] PermissÃµes salvas com sucesso.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PermissoesPopup] Erro ao salvar permissÃµes: {ex.Message}");
            }

            if (_isClosing) return; _isClosing = true; try { await _popup.CloseAsync(); } catch { }
        }
    }
}


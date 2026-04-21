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
    /// ViewModel para uma categoria de permissões no popup.
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
    /// ViewModel para o popup de permissões do usuário (DEBUG only).
    /// </summary>
    public partial class PermissoesPopupViewModel : ObservableObject
    {
        private readonly Popup _popup;

        [ObservableProperty]
        private ObservableCollection<PermissaoCategoriaViewModel> categorias = new();

        public PermissoesPopupViewModel(Popup popup)
        {
            _popup = popup;
            CarregarPermissoes();
        }

        /// <summary>
        /// Carrega todas as permissões do usuário via reflexão.
        /// </summary>
        private void CarregarPermissoes()
        {
            Categorias.Clear();

            var permissoes = Permissoes.UsuarioPermissoes;
            if (permissoes == null) return;

            // Permissões principais
            ExtrairPermissoes("Regionais", permissoes.regionais);
            ExtrairPermissoes("Propriedades", permissoes.propriedades);
            ExtrairPermissoes("Proprietários", permissoes.proprietarios);
            ExtrairPermissoes("Unidades Epidemiológicas", permissoes.unidadesEpidemiologicas);
            ExtrairPermissoes("Atividades", permissoes.atividades);

            // Lotes
            ExtrairPermissoes("Lotes", permissoes.lotes);

            // Monitoramento (dentro de Lotes)
            if (permissoes.lotes?.monitoramento != null)
            {
                ExtrairPermissoes("Lotes - Sanidade", permissoes.lotes.monitoramento.sanidade);
                ExtrairPermissoes("Lotes - Zootécnico", permissoes.lotes.monitoramento.zootecnico);
                ExtrairPermissoes("Lotes - ISI Macro", permissoes.lotes.monitoramento.isiMacro);
                ExtrairPermissoes("Lotes - Nutrição", permissoes.lotes.monitoramento.nutricao);
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
        /// Extrai todas as propriedades booleanas de um objeto de permissões.
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
        /// Formata o nome da propriedade para exibição amigável.
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
                "excluirForm" => "Excluir Formulário",
                _ => char.ToUpper(propName[0]) + propName.Substring(1)
            };
        }

        /// <summary>
        /// Fecha o popup sem salvar.
        /// </summary>
        [RelayCommand]
        private void Fechar()
        {
            _ = _popup?.CloseAsync();
        }

        /// <summary>
        /// Salva as permissões alteradas no Preferences e notifica a UI.
        /// </summary>
        [RelayCommand]
        private void Salvar()
        {
            try
            {
                // Aplica as alterações via reflexão
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

                // Notifica todas as propriedades estáticas via método público
                Permissoes.NotifyAllStaticPropertiesChanged();

                Debug.WriteLine("[PermissoesPopup] Permissões salvas com sucesso.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PermissoesPopup] Erro ao salvar permissões: {ex.Message}");
            }

            _ = _popup?.CloseAsync();
        }
    }
}

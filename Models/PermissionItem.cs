using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace SilvaData.Models
{
    /// <summary>
    /// Representa uma permissão individual para exibição e edição no popup de permissões.
    /// </summary>
    public class PermissionItem : ObservableObject
    {
        private bool _valor;

        /// <summary>
        /// Nome completo da permissão (ex: "Lotes - Consultar")
        /// </summary>
        public string Nome { get; set; }

        /// <summary>
        /// Categoria da permissão (ex: "Lotes", "Monitoramento", "Regionais")
        /// </summary>
        public string Categoria { get; set; }

        /// <summary>
        /// Nome da propriedade (ex: "consultar", "cadastrar")
        /// </summary>
        public string PropriedadeNome { get; set; }

        /// <summary>
        /// Valor atual da permissão (true/false)
        /// </summary>
        public bool Valor
        {
            get => _valor;
            set => SetProperty(ref _valor, value);
        }

        /// <summary>
        /// Objeto pai que contém a propriedade (para atualização)
        /// </summary>
        public object Parent { get; set; }

        /// <summary>
        /// Info da propriedade para atualização via reflexão
        /// </summary>
        public System.Reflection.PropertyInfo PropertyInfo { get; set; }

        public PermissionItem(string nome, string categoria, string propriedadeNome, bool valor, object parent, System.Reflection.PropertyInfo propertyInfo)
        {
            Nome = nome;
            Categoria = categoria;
            PropriedadeNome = propriedadeNome;
            _valor = valor;
            Parent = parent;
            PropertyInfo = propertyInfo;
        }
    }
}

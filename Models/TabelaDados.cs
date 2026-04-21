using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Text;

namespace SilvaData.Models
{
    public partial class TabelaDados : ObservableObject
    {
        [ObservableProperty]
        private string campo1;

        [ObservableProperty]
        private string valor1;

        [ObservableProperty]
        private string campo2;

        [ObservableProperty]
        private string valor2;

        [ObservableProperty]
        private string campo3;

        // Quando 'Valor3' mudar, notifique a UI para
        // recalcular 'AveNaoSaudavel' e 'AveNaoSaudavelWidth'.
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AveNaoSaudavel))]
        [NotifyPropertyChangedFor(nameof(AveNaoSaudavelWidth))]
        private string valor3;

        [ObservableProperty]
        private string campo4;

        [ObservableProperty]
        private string valor4;

        [ObservableProperty]
        private string campo5;

        [ObservableProperty]
        private string valor5;

        [ObservableProperty]
        private string campo6;

        [ObservableProperty]
        private string valor6;

        [ObservableProperty]
        private string campo7;

        [ObservableProperty]
        private string valor7;

        [ObservableProperty]
        private string campo8;

        [ObservableProperty]
        private string valor8;

        [ObservableProperty]
        private string campo9;

        [ObservableProperty]
        private string valor9;

        [ObservableProperty]
        private string campo10;

        [ObservableProperty]
        private string valor10;

        [ObservableProperty]
        private LoteForm loteForm;

        public bool AveNaoSaudavel => (Valor3 != null) && (int.TryParse(Valor3, out int val) && val >= 40);

        public int AveNaoSaudavelWidth => AveNaoSaudavel ? 100 : 70;
    }
}
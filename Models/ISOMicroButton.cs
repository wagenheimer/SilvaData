using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Text;

namespace SilvaData.Models
{
    public class ISIMicroButton : ObservableObject
    {
        public DateTime Data { get; set; }
        public LoteForm LoteForm;
    }

}

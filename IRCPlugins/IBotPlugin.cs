using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using System.Text;
using System.Threading.Tasks;

namespace IRCPlugin
{
    public interface IBotPlugin : IDisposable
    {
        string Name { get; set; }
        string Status { get; set; }
        event PropertyChangedHandler PropertyChanged;
        IList<string> Buttons { get; }
        void Open(string name);
    }
}

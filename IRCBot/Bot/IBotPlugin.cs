using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Bot
{
    public interface IBotPlugin : INotifyPropertyChanged, IDisposable
    {
        string Name { get; }
        string Status { get; }
        IList<string> Buttons { get; }
        Window Open(string name);
    }
}

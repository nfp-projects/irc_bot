using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Security.Cryptography;
using IRCPlugin;
using IRCBot.Bot;
using System.Windows.Threading;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IRCBot
{
    public class ControlWriter : System.IO.TextWriter
    {
        private string _buffer;
        private ObservableCollection<string> _list;
        private Dispatcher _dispatcher;

        public ControlWriter(ObservableCollection<string> list, Dispatcher dispatcher)
        {
            _buffer = "";
            _list = list;
            _dispatcher = dispatcher;
        }

        public override void Write(char value)
        {
            _buffer += value;
            Check();
        }

        public override void Write(string value)
        {
            _buffer += value;
            Check();
        }

        private async void Check()
        {
            await Task.Run(() =>
            {
                lock (_list)
                {
                    int index = _buffer.IndexOf('\n');
                    if (index > 0)
                    {
                        _dispatcher.Invoke(() =>
                        {
                            _list.Add(_buffer.Remove(index).Replace("\r", ""));
                            if (_list.Count > 50)
                                _list.RemoveAt(0);
                        });
                        _buffer = _buffer.Remove(0, index + 1);
                    }
                }
            });
            
        }

        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }
    }
}

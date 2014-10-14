using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using IRCPlugin;
using System.Windows;
using System.Windows.Threading;
using System.Text;
using System.Threading.Tasks;

namespace Debugger
{
    public class DebugPlugin : BotPlugin
    {
        private string _status;
        private ObservableCollection<string> _messages;
        private Dispatcher _dispatcher;

        public DebugPlugin(IIrcClient client)
            : base(client)
        {
            _status = "Loaded";
            _messages = new ObservableCollection<string>();
        }

        public override string Name
        {
            get { return "Debugger"; }
        }

        public override string Status
        {
            get { return _status; }
        }

        public override IList<string> Buttons
        {
            get { return new string[] { "Open" }; }
        }

        public ObservableCollection<string> Messages
        {
            get { return _messages; }
        }

        protected override void MessageRecieved(object sender, ChatSharp.Events.PrivateMessageEventArgs e)
        {
            _status = string.Format("[{0}] <{1}> {2}", e.PrivateMessage.Source, e.PrivateMessage.User.Nick, e.PrivateMessage.Message);
            if (_dispatcher != null)
                _dispatcher.Invoke(() =>
                {
                    _messages.Add(_status);
                    if (_messages.Count > 20)
                        _messages.RemoveAt(0);
                });
            else
            {
                _messages.Add(_status);
                if (_messages.Count > 20)
                    _messages.RemoveAt(0);
            }
            SendPropertyChanged("Status");
        }

        public override void Open(string name)
        {
            if (_window != null)
                return;
            DebugWindow w = new DebugWindow(this);
            _dispatcher = w.Dispatcher;
            OpenWindow(w);
        }
    }
}

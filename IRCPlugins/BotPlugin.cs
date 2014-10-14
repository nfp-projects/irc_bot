using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using IRCPlugin;
using System.Text;
using System.Threading.Tasks;

namespace IRCPlugin
{
    public abstract class BotPlugin : MarshalByRefObject, IBotPlugin
    {
        protected bool _disposed = false;
        protected IIrcClient _client;
        protected Window _window;

        public BotPlugin(IIrcClient client)
        {
            _client = client;

            if (_client != null && _client.Client != null)
            {
                _client.Client.UserMessageRecieved += Client_UserMessageRecieved;
                _client.Client.ChannelMessageRecieved += Client_ChannelMessageRecieved;
            }
        }

        void Client_ChannelMessageRecieved(object sender, ChatSharp.Events.PrivateMessageEventArgs e)
        {
            this.MessageRecieved(sender, e);
            this.ChannelMessageRecieved(sender, e);
        }

        void Client_UserMessageRecieved(object sender, ChatSharp.Events.PrivateMessageEventArgs e)
        {
            this.MessageRecieved(sender, e);
            this.PrivateMessageRecieved(sender, e);
        }

        protected virtual void ChannelMessageRecieved(object sender, ChatSharp.Events.PrivateMessageEventArgs e)
        {
        }

        protected virtual void PrivateMessageRecieved(object sender, ChatSharp.Events.PrivateMessageEventArgs e)
        {
        }

        protected virtual void MessageRecieved(object sender, ChatSharp.Events.PrivateMessageEventArgs e)
        {
        }

        protected void SendPropertyChanged(string name)
        {
            PropertyChanged(this, new PropertyChangedArgs(name));
        }

        public event PropertyChangedHandler PropertyChanged = (s, e) => { };

        public void Dispose()
        {
            if (_client != null && _client.Client != null)
            {
                _client.Client.UserMessageRecieved -= Client_UserMessageRecieved;
                _client.Client.ChannelMessageRecieved -= Client_ChannelMessageRecieved;
            }
            if (_window != null)
            {
                _window.Close();
            }

            _disposed = true;
        }

        public virtual string Name
        {
            get { throw new NotImplementedException(); }
        }

        public virtual string Status
        {
            get { throw new NotImplementedException(); }
        }

        public virtual IIrcClient Client
        {
            get { return _client; }
        }

        public virtual IList<string> Buttons
        {
            get { return new string[] { }; }
        }

        public virtual void Open(string name)
        {
        }

        protected void OpenWindow(Window w)
        {
            _window = w;
            _window.Show();
            _window.Closing += _window_Closing;
        }

        void _window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _window.DataContext = null;
            _window = null;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}

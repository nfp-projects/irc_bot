using System;
using System.Collections.Generic;
using System.Linq;
using StructureMap;
using System.Windows;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Bot
{
    public abstract class BotPlugin : IBotPlugin
    {
        protected bool _disposed = false;
        protected Bot.IrcClient _client;

        public BotPlugin()
        {
            _client = ObjectFactory.GetInstance<Bot.IrcClient>();

            if (_client.Client != null)
            {
                _client.Client.UserMessageRecieved += PrivateMessageRecieved;
                _client.Client.UserMessageRecieved += MessageRecieved;
                _client.Client.ChannelMessageRecieved += ChannelMessageRecieved;
                _client.Client.ChannelMessageRecieved += MessageRecieved;
            }
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
            PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(name));
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged = (s, e) => { };

        public void Dispose()
        {
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

        public virtual IList<string> Buttons
        {
            get { return new string[] { }; }
        }

        public virtual Window Open(string name)
        {
            return null;
        }
    }
}

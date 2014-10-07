using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatSharp;

namespace IRCBot.Bot
{
    public class IrcClient
    {
        private bool _connected = false;
        private ChatSharp.IrcClient _client;
        public event EventHandler Connected;

        public void Create(string server, string nick, int port, bool ssl)
        {
            _client = new ChatSharp.IrcClient(String.Format("{0}:{1}", server, port), new IrcUser(nick, nick), ssl);
            _client.RawMessageRecieved += _client_RawMessageRecieved;
        }

        void _client_RawMessageRecieved(object sender, ChatSharp.Events.RawMessageEventArgs e)
        {
            if (e.Message.IndexOf("/WHOIS") >= 0 && !_connected)
            {
                _connected = true;
                Connected(this, null);
            }
        }

        public void Connect()
        {
            _client.ConnectAsync();
        }

        public void Disconnect()
        {
            if (_client != null)
            {
                _client.NetworkStream.Dispose();
            }
        }

        public ChatSharp.IrcClient Client
        {
            get { return _client; }
        }
    }
}

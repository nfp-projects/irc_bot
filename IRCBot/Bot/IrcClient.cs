using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using IRCPlugin;
using System.Text;
using System.Threading.Tasks;
using ChatSharp;

namespace IRCBot.Bot
{
    public class IrcClient : MarshalByRefObject, INotifyPropertyChanged, IIrcClient
    {
        private bool _connected = false;
        private bool _connecting = false;
        private string _server;
        private string _nick;
        private string _password;
        private int _port;
        private bool _ssl;
        private bool _nickserv;
        private ChatSharp.IrcClient _client;
        public event EventHandler OnConnected;
        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };

        public void Create(string server, string nick, string password, int port, bool ssl, bool nickserv)
        {
            _server = server;
            _nick = nick;
            _password = password;
            _port = port;
            _ssl = ssl;
            _nickserv = nickserv;

            if (!string.IsNullOrEmpty(password) || nickserv)
                _client = new ChatSharp.IrcClient(String.Format("{0}:{1}", server, port), new IrcUser(nick, nick), ssl);
            else
                _client = new ChatSharp.IrcClient(String.Format("{0}:{1}", server, port), new IrcUser(nick, nick, password), ssl);
            _client.RawMessageRecieved += _client_RawMessageRecieved;
            _client.OnDisconnected += _client_OnDisconnected;
        }

        void _client_RawMessageRecieved(object sender, ChatSharp.Events.RawMessageEventArgs e)
        {
            if (e.Message.IndexOf("/MOTD") >= 0 && !_connected)
            {
                if (!_nickserv)
                {
                    Connected = true;
                    Connecting = false;
                    OnConnected(this, null);
                }
                else
                    _client.SendMessage("identify " + _password, "NickServ");
            }
            else if (e.Message[0] == ':' && e.Message.Contains(String.Format("MODE {0} :+r", _nick)))
            {
                Connected = true;
                Connecting = false;
                OnConnected(this, null);
            }
        }

        public void Connect()
        {
            _client.ConnectAsync();
            Connecting = true;
        }

        void _client_OnDisconnected(object sender, EventArgs e)
        {
            Disconnect();
        }

        public void Disconnect()
        {
            if (_client != null)
            {
                Connecting = Connected = false;
                _client.NetworkStream.Dispose();
            }
        }

        public ChatSharp.IrcClient Client
        {
            get { return _client; }
        }

        public bool Connected
        {
            get { return _connected; }
            set
            {
                _connected = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Connected"));
            }
        }
        public bool Connecting
        {
            get { return _connecting; }
            set
            {
                _connecting = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Connecting"));
            }
        }
    }
}

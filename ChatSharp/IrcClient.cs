using System;
using System.IO;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using ChatSharp.Events;
using System.Timers;
using ChatSharp.Handlers;

#pragma warning disable 4014
namespace ChatSharp
{
    public delegate bool CertificateError(object sender, X509Certificate cert,
            X509Chain chain,
            System.Net.Security.SslPolicyErrors errors);

    public partial class IrcClient : MarshalByRefObject
    {
        public delegate void MessageHandler(IrcClient client, IrcMessage message);

        private Dictionary<string, MessageHandler> Handlers { get; set; }
        private const int ReadBufferLength = 1024;
        private byte[] ReadBuffer { get; set; }
        private int ReadBufferIndex { get; set; }
        private string ServerHostname { get; set; }
        private int ServerPort { get; set; }
        private Timer PingTimer { get; set; }
        private ConcurrentQueue<string> WriteQueue { get; set; }
        private bool IsWriting { get; set; }
        private bool IsQuitting { get; set; }
        private string PartMessage { get; set; }

        internal string ServerNameFromPing { get; set; }

        public Socket Socket { get; set; }
        public Stream NetworkStream { get; set; }
        public bool UseSSL { get; private set; }
        public Encoding Encoding { get; set; }
        public IrcUser User { get; set; }
        public ChannelCollection Channels { get; private set; }
        public ClientSettings Settings { get; set; }
        public RequestManager RequestManager { get; set; }
        public ServerInfo ServerInfo { get; set; }

        public event CertificateError CertificateManualValidation;
        public event UnhandledExceptionEventHandler UnhandledException = (sender, e) => { };
        public event EventHandler OnDisconnected = (sender, e) => { };
        public event EventHandler<UnhandledExceptionEventArgs> NetworkError = (sender, e) => { };
        public event EventHandler<RawMessageEventArgs> RawMessageSent = (sender, e) => { };
        public event EventHandler<RawMessageEventArgs> RawMessageRecieved = (sender, e) => { };
        public event EventHandler<IrcNoticeEventArgs> NoticeRecieved = (sender, e) => { };
        public event EventHandler<ServerMOTDEventArgs> MOTDPartRecieved = (sender, e) => { };
        public event EventHandler<ServerMOTDEventArgs> MOTDRecieved = (sender, e) => { };
        public event EventHandler<PrivateMessageEventArgs> PrivateMessageRecieved = (sender, e) => { };
        public event EventHandler<PrivateMessageEventArgs> ChannelMessageRecieved = (sender, e) => { };
        public event EventHandler<PrivateMessageEventArgs> UserMessageRecieved = (sender, e) => { };
        public event EventHandler<ErronousNickEventArgs> NickInUse = (sender, e) => { };
        public event EventHandler<ModeChangeEventArgs> ModeChanged = (sender, e) => { };
        public event EventHandler<ChannelUserEventArgs> UserJoinedChannel = (sender, e) => { };
        public event EventHandler<ChannelUserEventArgs> UserPartedChannel = (sender, e) => { };
        public event EventHandler<ChannelEventArgs> ChannelListRecieved = (sender, e) => { };
        public event EventHandler<EventArgs> ConnectionComplete = (sender, e) => { };
        public event EventHandler<SupportsEventArgs> ServerInfoRecieved = (sender, e) => { };
        public event EventHandler<KickEventArgs> UserKicked = (sender, e) => { };

        public IrcClient(string serverAddress, IrcUser user, bool useSSL = false)
        {
            if (serverAddress == null) throw new ArgumentNullException("serverAddress");
            if (user == null) throw new ArgumentNullException("user");

            User = user;
            PartMessage = "";
            ServerAddress = serverAddress;
            Encoding = Encoding.UTF8;
            Channels = new ChannelCollection(this);
            Settings = new ClientSettings();
            Handlers = new Dictionary<string, MessageHandler>();
            MessageHandlers.RegisterDefaultHandlers(this);
            RequestManager = new RequestManager();
            UseSSL = useSSL;
            WriteQueue = new ConcurrentQueue<string>();
        }

        public void SetHandler(string message, MessageHandler handler)
        {
#if DEBUG
            // This is the default behavior if 3rd parties want to handle certain messages themselves
            // However, if it happens from our own code, we probably did something wrong
            if (Handlers.ContainsKey(message.ToUpper()))
                Console.WriteLine("Warning: {0} handler has been overwritten", message);
#endif
            message = message.ToUpper();
            Handlers[message] = handler;
        }

        internal static DateTime DateTimeFromIrcTime(int time)
        {
            return new DateTime(1970, 1, 1).AddSeconds(time);
        }

        public void ConnectAsync()
        {
            string temp;
            while (WriteQueue.TryDequeue(out temp)) ;
            IsQuitting = false;
            if (Socket != null && Socket.Connected) throw new InvalidOperationException("Socket is already connected to server.");
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ReadBuffer = new byte[ReadBufferLength];
            ReadBufferIndex = 0;
            Socket.BeginConnect(ServerHostname, ServerPort, ConnectComplete, null);
            PingTimer = new Timer(30000);
            PingTimer.Elapsed += (sender, e) => 
            {
                if (!string.IsNullOrEmpty(ServerNameFromPing))
                    SendRawMessage("PING :{0}", ServerNameFromPing);
            };
        }

        public void Quit()
        {
            Quit(null);
        }

        public async void Quit(string reason)
        {
            if (IsQuitting)
                return;
            IsQuitting = true;

            if (reason == null)
                await SendRawMessage("QUIT");
            else
                await SendRawMessage("QUIT :{0}", reason);

            Socket.BeginDisconnect(false, ar =>
            {
                Socket.EndDisconnect(ar);
                NetworkStream.Close();
                NetworkStream.Dispose();
                NetworkStream = null;
                Socket.Dispose();
                if (OnDisconnected != null)
                    OnDisconnected(this, new EventArgs());
            }, null);
            PingTimer.Dispose();
        }

        private void ConnectComplete(IAsyncResult result)
        {
            Socket.EndConnect(result);

            NetworkStream = new NetworkStream(Socket);
            if (UseSSL)
            {
                NetworkStream = new SslStream(NetworkStream, false, new RemoteCertificateValidationCallback( CheckCertificate));
                try
                {
                    ((SslStream)NetworkStream).AuthenticateAsClient(ServerHostname);
                }
                catch (Exception)
                {
                    NetworkStream.Dispose();
                    Socket.Dispose();
                    if (OnDisconnected != null)
                        OnDisconnected(this, new EventArgs());
                    return;
                }
            }

            Read();
            
            if (!string.IsNullOrEmpty(User.Password))
                SendRawMessage("PASS {0}", User.Password);
            SendRawMessage("NICK {0}", User.Nick);
            // hostname, servername are ignored by most IRC servers
            SendRawMessage("USER {0} hostname servername :{1}", User.User, User.RealName);
            PingTimer.Start();
        }

        private bool CheckCertificate(
            object sender, X509Certificate cert,
            X509Chain chain,
            System.Net.Security.SslPolicyErrors errors)
        {
            if (errors != System.Net.Security.SslPolicyErrors.None && CertificateManualValidation != null)
            {
                return this.CertificateManualValidation(this, cert, chain, errors);
            }
            return false;
        }

        private async void Read()
        {
            while (true)
            {
                int length = 0;
                try
                {
                    length = await NetworkStream.ReadAsync(ReadBuffer, ReadBufferIndex, ReadBuffer.Length);
                }
                catch (Exception e)
                {
                    if (NetworkStream == null)
                        return;
                    OnNetworkError(new UnhandledExceptionEventArgs(e, true));
                    Quit();
                    return;
                }
                string message = PartMessage + Encoding.GetString(ReadBuffer, 0, length).Replace("\r", "");

                while (true)
                {
                    if (message.IndexOf('\n') == -1)
                    {
                        PartMessage = message;
                        break;
                    }
                    var parts = message.Split(new char[] { '\n' }, 2);
                    HandleMessage(parts[0]);
                    message = parts[1];
                }
            }
        }

        private void HandleMessage(string rawMessage)
        {
            OnRawMessageRecieved(new RawMessageEventArgs(rawMessage, false));
            var message = new IrcMessage(rawMessage);
            if (Handlers.ContainsKey(message.Command.ToUpper()))
                Handlers[message.Command.ToUpper()](this, message);
            else
            {
                // TODO: Fire an event or something
            }
        }

        public async Task SendRawMessage(string message, params object[] format)
        {
            message = string.Format(message, format);

            lock (WriteQueue)
            {
                if (IsWriting)
                {
                    WriteQueue.Enqueue(message);
                    return;
                }
                IsWriting = true;
            }

            var data = Encoding.GetBytes(message + "\r\n");

            try
            {
                await NetworkStream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception e)
            {
                if (NetworkStream == null)
                    return;
                OnNetworkError(new UnhandledExceptionEventArgs(e, true));
                Quit();
                return;
            }
            
            OnRawMessageSent(new RawMessageEventArgs(message, true));

            lock (WriteQueue)
            {
                IsWriting = false;
            }
            if (WriteQueue.TryDequeue(out message))
                await SendRawMessage(message);
        }

        public string ServerAddress
        {
            get
            {
                return ServerHostname + ":" + ServerPort;
            }
            internal set
            {
                string[] parts = value.Split(':');
                if (parts.Length > 2 || parts.Length == 0)
                    throw new FormatException("Server address is not in correct format ('hostname:port')");
                ServerHostname = parts[0];
                if (parts.Length > 1)
                    ServerPort = int.Parse(parts[1]);
                else
                    ServerPort = 6667;
            }
        }

        public async Task SendIrcMessage(IrcMessage message)
        {
            await SendRawMessage(message.RawMessage);
        }

        protected internal virtual void OnNetworkError(UnhandledExceptionEventArgs e)
        {
            try
            {
                NetworkError(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnRawMessageSent(RawMessageEventArgs e)
        {
            try
            {
                RawMessageSent(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnRawMessageRecieved(RawMessageEventArgs e)
        {
            try
            {
                RawMessageRecieved(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnNoticeRecieved(IrcNoticeEventArgs e)
        {
            try
            {
                NoticeRecieved(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnMOTDPartRecieved(ServerMOTDEventArgs e)
        {
            try
            {
                MOTDPartRecieved(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnMOTDRecieved(ServerMOTDEventArgs e)
        {
            try
            {
                MOTDRecieved(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnPrivateMessageRecieved(PrivateMessageEventArgs e)
        {
            try
            {
                PrivateMessageRecieved(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnChannelMessageRecieved(PrivateMessageEventArgs e)
        {
            try
            {
                ChannelMessageRecieved(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnUserMessageRecieved(PrivateMessageEventArgs e)
        {
            try
            {
                UserMessageRecieved(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnNickInUse(ErronousNickEventArgs e)
        {
            try
            {
                NickInUse(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnModeChanged(ModeChangeEventArgs e)
        {
            try
            {
                ModeChanged(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnUserJoinedChannel(ChannelUserEventArgs e)
        {
            try
            {
                UserJoinedChannel(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnUserPartedChannel(ChannelUserEventArgs e)
        {
            try
            {
                UserPartedChannel(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnChannelListRecieved(ChannelEventArgs e)
        {
            try
            {
                ChannelListRecieved(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnConnectionComplete(EventArgs e)
        {
            try
            {
                ConnectionComplete(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnServerInfoRecieved(SupportsEventArgs e)
        {
            try
            {
                ServerInfoRecieved(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
        protected internal virtual void OnUserKicked(KickEventArgs e)
        {
            try
            {
                UserKicked(this, e);
            }
            catch (Exception err)
            {
                UnhandledException(this, new UnhandledExceptionEventArgs(err, false));
            }
        }
    }
}

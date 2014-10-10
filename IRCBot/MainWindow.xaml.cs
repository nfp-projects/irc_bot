using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Security.Cryptography;
using StructureMap;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IRCBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isMoving;
        private Point _lastPoint;
        private Bot.IrcClient _client;
        private X509Store _certStorage;
        private ObservableCollection<Bot.IBotPlugin> _plugins;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            this.DataContext = this;
            _isMoving = false;
            _plugins = new ObservableCollection<Bot.IBotPlugin>();
            listPlugins.ItemsSource = _plugins;

            _client = new Bot.IrcClient();
            _client.OnConnected += _client_Connected;

            gridMain.DataContext = loginPanel.DataContext = _client;
            var name = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            _certStorage = new X509Store(name, StoreLocation.CurrentUser);
            _certStorage.Open(OpenFlags.ReadWrite);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _certStorage.Close();
            Properties.Settings.Default.Save();
            _client.Disconnect();
        }

        private void loginPanel_Connect(object sender, EventArgs ea)
        {
            _client.Create(Properties.Settings.Default.server, Properties.Settings.Default.nick, Properties.Settings.Default.password, int.Parse(Properties.Settings.Default.port), Properties.Settings.Default.ssl, Properties.Settings.Default.nickserv);
            _client.Client.CertificateManualValidation += Client_VerifyCertificate;
            _client.Client.NetworkError += (s, e) => Console.WriteLine("Error: " + e.SocketError);
            _client.Client.RawMessageRecieved += Client_RawMessageRecieved;
            _client.Client.RawMessageSent += (s, e) => Console.WriteLine(">> {0}", e.Message);
            _client.Client.UserMessageRecieved += (s, e) =>
            {
                Console.WriteLine("<{0}> {1}", e.PrivateMessage.User.Nick, e.PrivateMessage.Message);
            };
            _client.Client.ChannelMessageRecieved += (s, e) =>
            {
                Console.WriteLine("<{0}> {1}", e.PrivateMessage.User.Nick, e.PrivateMessage.Message);
            };
            _client.Connect();
        }

        void Client_RawMessageRecieved(object sender, ChatSharp.Events.RawMessageEventArgs e)
        {
            if (this.CheckAccess())
                textblockStatus.Text = e.Message;
            else
                this.Dispatcher.Invoke(() => { textblockStatus.Text = e.Message; });
        }

        void _client_Connected(object sender, EventArgs e)
        {
            _client.Client.Channels.Join("#nfp");
            _client.Client.Channels.Join("#nfp-staff");
            if (this.CheckAccess())
                buttonRefresh_Click(null, null);
            else
                this.Dispatcher.Invoke(new RoutedEventHandler(buttonRefresh_Click), null, null);
        }

        bool Client_VerifyCertificate(object sender, X509Certificate cert, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors errors)
        {
            if (errors == System.Net.Security.SslPolicyErrors.None)
                return true;

            for (int i = 0; i < _certStorage.Certificates.Count; i++)
                if (_certStorage.Certificates[i].Equals(cert))
                    return true;

            var d = Application.Current.Dispatcher;
            bool isValid = false;
            if (d.CheckAccess())
                isValid = verifyCertificate(cert);
            else
                d.Invoke(() => {
                    isValid = verifyCertificate(cert);
                });

            return isValid;
        }

        bool verifyCertificate(X509Certificate cert)
        {
            var verify = new VerifyCertificate(cert);
            verify.ShowDialog();

            if (verify.Valid && verify.Save)
            {
                _certStorage.Add(cert as X509Certificate2);
            }
            return verify.Valid;
        }

        private void buttonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _client.Disconnect();
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            ObjectFactory.Initialize(x =>
            {
                x.For<Bot.IrcClient>().Singleton().Use(_client);
                x.AddRegistry<Bot.Registry>();
                x.AddRegistry<Bot.Debug.DebugRegistry>();
            });
            IList<Bot.IBotPlugin> newList = ObjectFactory.GetAllInstances<Bot.IBotPlugin>();

            for (int i = 0; i < _plugins.Count; i++)
                _plugins[i].Dispose();
            _plugins.Clear();

            for (int i = 0; i < newList.Count; i++)
                _plugins.Add(newList[i]);
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonPlugin_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Bot.IBotPlugin plugin = button.Tag as Bot.IBotPlugin;
            Window w = plugin.Open(button.DataContext as string);
            if (w != null)
                w.ShowDialog();
        }
    }
}

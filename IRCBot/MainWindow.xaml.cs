using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Security.Cryptography;
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

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            _client = new Bot.IrcClient();
            _client.Connected += _client_Connected;
            _isMoving = false;

            var name = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            _certStorage = new X509Store(name, StoreLocation.CurrentUser);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
            _client.Disconnect();
        }

        private void loginPanel_Connect(object sender, EventArgs ea)
        {
            _client.Create(Properties.Settings.Default.server, Properties.Settings.Default.nick, int.Parse(Properties.Settings.Default.port), Properties.Settings.Default.ssl);
            _client.Client.CertificateManualValidation += Client_VerifyCertificate;
            _client.Client.NetworkError += (s, e) => Console.WriteLine("Error: " + e.SocketError);
            _client.Client.RawMessageRecieved += (s, e) => Console.WriteLine("<< {0}", e.Message);
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

        void _client_Connected(object sender, EventArgs e)
        {
            _client.Client.Channels.Join("#nfp");
            _client.Client.Channels.Join("#nfp-staff");
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
                //Throws "The X509 certificate store has not been opened"
                //_certStorage.Add(cert as X509Certificate2);
            }
            return verify.Valid;
        }
    }
}

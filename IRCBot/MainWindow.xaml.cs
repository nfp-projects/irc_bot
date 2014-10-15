using System;
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
using System.Reflection;
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
        private PluginManager _manager;
        private AppDomainSetup _setup;
        private IrcClient _client;
        private X509Store _certStorage;
        private bool reconnect = false;
        private ObservableCollection<Bot.PluginContainer> _plugins;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {
            _setup = new AppDomainSetup();
            _client = new Bot.IrcClient();
            _setup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            _manager = new PluginManager(_setup, _client);
            _manager.UnhandledException += _manager_UnhandledException;

            listPlugins.ItemsSource = _manager.Plugins;
            this.DataContext = _client;

            _client.OnConnected += _client_Connected;
            _client.UnhandledException += _client_UnhandledException;

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
            _client.Client.NetworkError += Client_NetworkError;
            _client.Client.RawMessageRecieved += Client_RawMessageRecieved;
            _client.Client.RawMessageSent += (s, e) => Console.WriteLine("{0} >> {1}", DateTime.Now.ToShortTimeString(), e.Message);
            _client.Client.UserMessageRecieved += (s, e) =>
            {
                Console.WriteLine("{0} <{1}> {2}", DateTime.Now.ToShortTimeString(), e.PrivateMessage.User.Nick, e.PrivateMessage.Message);
            };
            _client.Client.ChannelMessageRecieved += (s, e) =>
            {
                Console.WriteLine("{0} <{1}> {2}", DateTime.Now.ToShortTimeString(), e.PrivateMessage.User.Nick, e.PrivateMessage.Message);
            };
            _client.Connect();
        }

        void Client_NetworkError(object sender, UnhandledExceptionEventArgs e)
        {
            if (reconnect)
                return;

            reconnect = true;

            this.Dispatcher.Invoke(async () =>
            {
                await Task.Delay(1000);
                string message = string.Format("{0} Error: {1}", DateTime.Now.ToShortTimeString(), (e.ExceptionObject as Exception).Message);

                if (e.IsTerminating)
                    message += " Reconnecting...";
                else
                    message += " Not reconnecting.";
                textblockStatus.Text = message;

                await Task.Delay(9000);

                reconnect = false;
                if (e.IsTerminating)
                    this._client.Connect();
            });
        }

        void Client_RawMessageRecieved(object sender, ChatSharp.Events.RawMessageEventArgs e)
        {
            this.Dispatcher.Invoke(() => {
                Console.WriteLine("{0} << {1}", DateTime.Now.ToShortTimeString(), e.Message);
                textblockStatus.Text = e.Message;
            });
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

        void _client_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ShowError(e.ExceptionObject as Exception, "Unknown error in client");
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _manager.ReloadPlugins();
            }
            catch (Exception err)
            {
                ShowError(err, "Error while reloading plugins.");
            }
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonPlugin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = sender as Button;
                Bot.PluginContainer plugin = button.Tag as Bot.PluginContainer;
                plugin.Open(button.DataContext as string);
            }
            catch (Exception err)
            {
                ShowError(err, "Error while opening window or in window");
            }
        }

        private void buttonUnload_Click(object sender, RoutedEventArgs e)
        {
            _manager.UnloadPlugins();
        }

        private void buttonPluginUnload_Click(object sender, RoutedEventArgs e)
        {
            PluginContainer plugin = (sender as Button).DataContext as PluginContainer;
            plugin.Dispose();
            _manager.Plugins.Remove(plugin);
        }

        void _manager_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var plugin = sender as PluginContainer;
            ShowError(e.ExceptionObject as Exception, "Problem with plugin " + plugin.Name);
            if (e.IsTerminating)
            {
                plugin.Dispose();
                _manager.Plugins.Remove(plugin);
            }
        }

        private void ShowError(Exception error, string message)
        {
            Error w = new Error(error, message);
            w.Show();
        }
    }
}

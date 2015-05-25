using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using IRCBot.Bot;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IRCBot
{
    /// <summary>
    /// Interaction logic for DebugWindow.xaml
    /// </summary>
    public partial class DebugWindow : Window
    {
        readonly IrcClient _client;
        readonly ObservableCollection<string> _debug;
        readonly List<string> _previousMessages;
        int _index;
        string _temp;

        public DebugWindow(IrcClient client, ObservableCollection<string> debug)
        {
            InitializeComponent();

            _client = client;
            itemsList.ItemsSource = _debug = debug;
            _previousMessages = new List<string>();
            _index = 1;
            scroller.ScrollToBottom();
        }
         
        private void textboxMessage_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                buttonSend_Click(null, null);
            if (e.Key == Key.Up && _index > 0)
            {
                if (_index == _previousMessages.Count)
                    _temp = textboxMessage.Text;
                textboxMessage.Text = _previousMessages[--_index];
                textboxMessage.CaretIndex = textboxMessage.Text.Length;
            }
            if (e.Key == Key.Down && _index < _previousMessages.Count)
            {
                if (++_index < _previousMessages.Count)
                    textboxMessage.Text = _previousMessages[_index];
                else
                    textboxMessage.Text = _temp;
                textboxMessage.CaretIndex = textboxMessage.Text.Length;
            }
        }

        private void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            var message = textboxMessage.Text;
            _previousMessages.Add(message);
            _index = _previousMessages.Count;
            textboxMessage.Text = "";

            if (message.Length > 0)
            {
                if (message.IndexOf("/msg ") == 0)
                {
                    string[] split = message.Split(new char[] { ' ' }, 3);
                    _client.Client.SendMessage(split[2], split[1]);
                    _debug.Add(string.Format("[{0}] <{1}> {2}", split[1], _client.Client.User.Nick, split[2]));
                }
                else if (message.IndexOf("/nick ") == 0)
                {
                    string[] split = message.Split(new char[] { ' ' }, 2);
                    _client.Client.Nick(split[1]);
                }
                else if (message.IndexOf("/join #") == 0)
                {
                    string[] split = message.Split(new char[] { ' ' }, 2);
                    _client.Client.JoinChannel(split[1]);
                }
                else if (message.IndexOf("/part #") == 0)
                {
                    string[] split = message.Split(new char[] { ' ' }, 2);
                    _client.Client.PartChannel(split[1]);
                }
                else if (message[0] == '#' && message.IndexOf(' ') > 2 && _client.Client != null)
                {
                    string[] split = message.Split(new char[] { ' ' }, 2);
                    _client.Client.SendMessage(split[1], split[0]);
                    _debug.Add(string.Format("[{0}] <{1}> {2}", split[0], _client.Client.User.Nick, split[1]));
                    textboxMessage.Text = string.Format("{0} ", split[0]);
                    textboxMessage.CaretIndex = textboxMessage.Text.Length;
                }
                else
                {
                    _debug.Add("» Invalid or unsupported command «");
                }
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonHelp_Click(object sender, RoutedEventArgs e)
        {
            panelHelp.Visibility = System.Windows.Visibility.Visible;
        }

        private void buttonPanelClose_Click(object sender, RoutedEventArgs e)
        {
            panelHelp.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}

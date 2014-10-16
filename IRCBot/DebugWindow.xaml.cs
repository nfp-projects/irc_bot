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

        public DebugWindow(IrcClient client, ObservableCollection<string> debug)
        {
            InitializeComponent();

            _client = client;
            itemsList.ItemsSource = _debug = debug;
            scroller.ScrollToBottom();
        }
         
        private void textboxMessage_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                buttonSend_Click(null, null);
        }

        private void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            var message = textboxMessage.Text;
            if (message.Length > 0)
                if (message[0] == '#' && message.IndexOf(' ') > 2 && _client.Client != null)
                {
                    string[] split = message.Split(new char[] { ' ' }, 2);
                    _client.Client.SendMessage(split[1], split[0]);
                    _debug.Add(string.Format("[{0}] <{1}> {2}", split[0], _client.Client.User.Nick, split[1]));
                    textboxMessage.Text = textboxMessage.Text.Remove(split[0].Length + 2);
                }
                else
                {
                    _debug.Add("» message must be: #<channel_name> message «");
                }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

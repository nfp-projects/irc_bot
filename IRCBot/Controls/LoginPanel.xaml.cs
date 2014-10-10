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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace IRCBot
{
    /// <summary>
    /// Interaction logic for LoginPanel.xaml
    /// </summary>
    public partial class LoginPanel : UserControl
    {
        public LoginPanel()
        {
            InitializeComponent();
        }

        public event EventHandler Connect;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Storyboard errorAnimation = textboxNick.Template.Resources["ErrorAnimation"] as Storyboard;;
            bool valid = true;
            int test;

            if (textboxServer.Text == "")
            {
                errorAnimation.Begin(textboxServer, textboxServer.Template);
                valid = false;
            }
            if (textboxPort.Text == "")
            {
                errorAnimation.Begin(textboxPort, textboxPort.Template);
                valid = false;
            }
            if (!int.TryParse(textboxPort.Text, out test))
            {
                errorAnimation.Begin(textboxPort, textboxPort.Template);
                valid = false;
            }
            if (textboxNick.Text == "")
            {
                errorAnimation.Begin(textboxNick, textboxNick.Template);
                valid = false;
            }

            if (valid && Connect != null)
            {
                Connect(null, null);
            }
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            var temp = this.DataContext;
        }
    }
}

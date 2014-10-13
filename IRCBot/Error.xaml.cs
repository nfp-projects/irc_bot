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
using System.Windows.Shapes;

namespace IRCBot
{
    /// <summary>
    /// Interaction logic for Error.xaml
    /// </summary>
    public partial class Error : Window
    {
        public Error(Exception error, string message)
        {
            InitializeComponent();

            textblockErrorMessage.Text = string.Format("{0}\n\n{1}", message, error.Message);

            Exception inner = error.InnerException;

            while (inner != null)
            {
                textblockErrorMessage.Text += "\n -> " + inner.Message;
                inner = inner.InnerException;
            }

            textblockErrorMessage.Text += "\n\n\n" + error.StackTrace;
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

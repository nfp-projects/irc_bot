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

namespace Debugger
{
    /// <summary>
    /// Interaction logic for DebugWindow.xaml
    /// </summary>
    public partial class DebugWindow : Window
    {
        private bool _isMoving;
        private Point _lastPoint;
        private DebugPlugin _plugin;

        public DebugWindow(DebugPlugin plugin)
        {
            InitializeComponent();

            _plugin = plugin;
            DataContext = _plugin;
        }

        private void textboxMessage_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                buttonSend_Click(null, null);
        }

        #region Default

        private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var window = button.DataContext as Window;
            window.WindowState = System.Windows.WindowState.Minimized;
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Window window = (sender as Border).DataContext as Window;
            _isMoving = true;
            _lastPoint = e.GetPosition(window);
            (sender as Border).CaptureMouse();
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isMoving)
            {
                Window window = (sender as Border).DataContext as Window;
                Point current = window.PointToScreen(e.GetPosition(window));
                window.Top = current.Y - this._lastPoint.Y;
                window.Left = current.X - this._lastPoint.X;
            }
        }

        private void Window_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Window window = (sender as Border).DataContext as Window;
            _isMoving = false;
            (sender as Border).ReleaseMouseCapture();
        }

        #endregion

        private void buttonSend_Click(object sender, RoutedEventArgs e)
        {
            var message = textboxMessage.Text;
            if (message.Length > 0 && message[0] == '#' && message.IndexOf(' ') > 2)
            {
                string[] split = message.Split(new char[] { ' ' }, 2);
                _plugin.Client.Client.SendMessage(split[1], split[0]);
                _plugin.Messages.Add(string.Format("[{0}] <{1}> {2}", split[0], _plugin.Client.Client.User.Nick, split[1]));
                textboxMessage.Text = "";
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.DataContext = null;
            
        }
    }
}

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

namespace AsunaPlugin
{
    /// <summary>
    /// Interaction logic for about.xaml
    /// </summary>
    public partial class about : Window
    {
        private bool _isMoving;
        private Point _lastPoint;

        public about()
        {
            InitializeComponent();
        }

        private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var window = button.DataContext as Window;
            window.WindowState = System.Windows.WindowState.Minimized;
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var window = button.DataContext as Window;
            window.Close();
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
    }
}

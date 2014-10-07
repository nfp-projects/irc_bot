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

namespace IRCBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isMoving;
        private Point _lastPoint;

        public MainWindow()
        {
            InitializeComponent();

            _isMoving = false;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isMoving = true;
            _lastPoint = e.GetPosition(this);
            this.CaptureMouse();
            base.OnMouseDown(e);
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isMoving = false;
            this.ReleaseMouseCapture();
            base.OnMouseUp(e);
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMoving)
            {
                Point current = this.PointToScreen(e.GetPosition(this));
                this.Top = current.Y - this._lastPoint.Y;
                this.Left = current.X - this._lastPoint.X;
            }

            base.OnMouseMove(e);
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }
    }
}

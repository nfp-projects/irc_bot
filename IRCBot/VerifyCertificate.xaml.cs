using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Security.Cryptography.X509Certificates;
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
    /// Interaction logic for VerifyCertificate.xaml
    /// </summary>
    public partial class VerifyCertificate : Window
    {
        X509Certificate _certificate;

        public bool Valid = false;
        public bool Save = false;

        public VerifyCertificate(X509Certificate cert)
        {
            InitializeComponent();

            _certificate = cert;
            var issuer = cert.Issuer.Split(',').Where(x => x.Contains("=")).Select(x => x.Trim().Split('=')).ToDictionary(x => x[0], x => x[1]);
            var subject = cert.Subject.Split(',').Where(x => x.Contains("=")).Select(x => x.Trim().Split('=')).ToDictionary(x => x[0], x => x[1]);

            textIssuedCN.Text = subject.ContainsKey("CN") ? subject["CN"] : "<Not Part Of Certificate>";
            textIssuedO.Text = subject.ContainsKey("O") ? subject["O"] : "<Not Part Of Certificate>";
            textIssuedOU.Text = subject.ContainsKey("OU") ? subject["OU"] : "<Not Part Of Certificate>";
            textIssuedSN.Text = cert.GetSerialNumberString().Aggregate("", (result, c) => result += ((!string.IsNullOrEmpty(result) && (result.Length + 1) % 3 == 0) ? ":" : "") + c.ToString());

            textIssuerCN.Text = issuer.ContainsKey("CN") ? issuer["CN"] : "<Not Part Of Certificate>";
            textIssuerO.Text = issuer.ContainsKey("O") ? issuer["O"] : "<Not Part Of Certificate>";
            textIssuerOU.Text = issuer.ContainsKey("OU") ? issuer["OU"] : "<Not Part Of Certificate>";

            X509Certificate2 temp = cert as X509Certificate2;

            textValidBegins.Text = temp.NotBefore.ToShortDateString();
            textValidEnds.Text = temp.NotAfter.ToShortDateString();
            textSha1Thumbprint.Text = temp.Thumbprint.Aggregate("", (result, c) => result += ((!string.IsNullOrEmpty(result) && (result.Length + 1) % 3 == 0) ? ":" : "") + c.ToString());
        }

        private void buttonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void buttonConnect_Click(object sender, RoutedEventArgs e)
        {
            this.Valid = true;
            this.Save = (bool)checkboxSave.IsChecked;
            this.Close();
        }
    }
}

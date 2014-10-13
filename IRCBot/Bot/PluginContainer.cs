using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using IRCPlugin;
using System.Windows;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Bot
{
    public class PluginContainer : MarshalByRefObject, INotifyPropertyChanged, IDisposable
    {
        private AppDomain _domain;
        private string _name;
        private IrcClient _client;
        IBotPlugin _plugin;

        public PluginContainer(AppDomainSetup setup, IrcClient client, string name)
        {
            _client = client;
            var splittet = name.Split('\\');
            _domain = AppDomain.CreateDomain(splittet[splittet.Length - 1], null, setup);
            _name = name;
        }

        public void Load()
        {
            var test = AppDomain.CurrentDomain;
            PluginLoader loader = (PluginLoader)_domain.CreateInstanceAndUnwrap(typeof(PluginLoader).Assembly.FullName, typeof(PluginLoader).FullName);
            _plugin = loader.Load(_client, _name);

            if (_plugin != null)
            {
                _plugin.PropertyChanged += _plugin_PropertyChanged;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get { return _plugin.Name; }
        }

        public string Status
        {
            get { return _plugin.Status; }
        }

        public string PluginName
        {
            get { return _name; }
        }

        public IList<string> Buttons
        {
            get { return _plugin.Buttons; }
        }

        public bool Loaded
        {
            get { return _plugin != null; }
        }

        void _plugin_PropertyChanged(object sender, PropertyChangedArgs e)
        {
            var name = e.PropertyName;
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose()
        {
            _plugin.Dispose();
            AppDomain.Unload(_domain);
            _plugin = null;
        }

        public void Open(string name)
        {
            _plugin.Open(name);
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }
}
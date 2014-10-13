using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Bot
{
    public class PluginManager : MarshalByRefObject
    {
        AppDomainSetup _setup;
        IrcClient _client;
        ObservableCollection<PluginContainer> _plugins;

        public PluginManager(AppDomainSetup setup, IrcClient client)
        {
            _client = client;
            _setup = setup;
            _plugins = new ObservableCollection<PluginContainer>();
        }

        public void ReloadPlugins()
        {
            UnloadPlugins();

            foreach (string file in Directory.GetFiles("plugins"))
            {
                bool found = false;
                for (int i = 0; i < _plugins.Count; i++)
                {
                    found = found || _plugins[i].PluginName == file;
                }

                if (found)
                    continue;

                var plugin = new PluginContainer(_setup, _client, file);
                plugin.Load();
                if (plugin.Loaded)
                    _plugins.Add(plugin);
            }
        }

        public void UnloadPlugins()
        {
            for (int i = 0; i < _plugins.Count; i++)
            {
                _plugins[i].Dispose();
            }
            _plugins.Clear();
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public ObservableCollection<PluginContainer> Plugins
        {
            get { return _plugins; }
        }
    }
}

﻿using System;
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
        public event UnhandledExceptionEventHandler UnhandledException = (s, e) => { };

        public PluginManager(AppDomainSetup setup, IrcClient client)
        {
            _client = client;
            _setup = setup;
            _plugins = new ObservableCollection<PluginContainer>();
        }

        public void ReloadPlugins()
        {
            foreach (string file in Directory.GetFiles("plugins"))
            {
                if (!file.EndsWith(".dll"))
                    continue;

                bool found = false;
                for (int i = 0; i < _plugins.Count; i++)
                {
                    found = found || _plugins[i].PluginName == file;
                }

                if (found)
                    continue;

                PluginContainer plugin;
                try
                {
                    plugin = new PluginContainer(_setup, _client, file);
                    plugin.Load();
                }
                catch (Exception e)
                {
                    plugin_UnhandledException(this, new UnhandledExceptionEventArgs(e, false));
                    continue;
                }

                if (plugin.Loaded)
                {
                    if (_client != null && _client.Client != null)
                    {
                        for (int i = 0; i < _client.Client.Channels.Count; i++)
                        {
                            _client.Client.SendMessage(string.Format("§ Plugin {0} Loaded §", plugin.Name), _client.Client.Channels[i].Name);
                        }
                    }
                    plugin.UnhandledException += plugin_UnhandledException;
                    _plugins.Add(plugin);
                }
            }
        }

        void plugin_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledException(sender, e);
        }

        public void UnloadPlugins()
        {
            for (int i = 0; i < _plugins.Count; i++)
            {
                for (int a = 0; a < _client.Client.Channels.Count; a++)
                {
                    _client.Client.SendMessage(string.Format("§ Plugin {0} Unloaded §", _plugins[i].Name), _client.Client.Channels[a].Name);
                }
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

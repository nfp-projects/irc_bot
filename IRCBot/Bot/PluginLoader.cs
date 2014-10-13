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
    public class PluginLoader : MarshalByRefObject
    {
        public IBotPlugin Load(IIrcClient client, string name)
        {
            var domain = AppDomain.CurrentDomain;
            domain.AssemblyResolve += domain_AssemblyResolve;
            var temp = Directory.GetCurrentDirectory();
            var assemply = domain.Load(name);
            var type = (from t in assemply.GetTypes()
                        where t.GetInterfaces().Contains(typeof(IBotPlugin))
                        select t).FirstOrDefault();

            if (type == null)
                return null;

            var constructor = type.GetConstructor(new Type[] { typeof(IIrcClient) });
            return constructor.Invoke(new object[] { client }) as IBotPlugin;
        }

        System.Reflection.Assembly domain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assembly = Assembly.LoadFile(String.Format("{0}\\{1}", Directory.GetCurrentDirectory(), args.Name));
            return assembly;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using StructureMap.Configuration.DSL;
using System.Text;
using System.Threading.Tasks;

namespace IRCBot.Bot
{
    class Registry : StructureMap.Configuration.DSL.Registry
    {
        public Registry()
        {
            Scan(y =>
            {
                //Find valid directories in the application path and load all plugins within it.
                if (Directory.Exists("plugin"))
                    y.AssembliesFromPath("plugin");
                if (Directory.Exists("plugins"))
                    y.AssembliesFromPath("plugins");
                //Look for Registry classes found in any of the plugins to register it with StructureMap.
                y.LookForRegistries();
            });
        }
    }
}

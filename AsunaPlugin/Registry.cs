using System;
using System.Collections.Generic;
using System.Linq;
using IRCBot.Bot;
using System.Text;
using System.Threading.Tasks;

namespace AsunaPlugin
{
    public class Registry : StructureMap.Configuration.DSL.Registry
    {
        public Registry()
        {
            For<IBotPlugin>().Singleton().Use<AsunaPlugin>();
        }
    }
}

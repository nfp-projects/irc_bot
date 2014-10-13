using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCPlugin
{
    public interface IIrcClient
    {
        event EventHandler OnConnected;
        void Create(string server, string nick, string password, int port, bool ssl, bool nickserv);
        void Connect();
        void Disconnect();
        ChatSharp.IrcClient Client { get; }
        bool Connected { get; set; }
        bool Connecting { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using IRCPlugin;
using System.Text;
using System.Threading.Tasks;

namespace AsunaPlugin
{
    public class AsunaPlugin : BotPlugin
    {
        private string _status;
        private string[] _mean = new string[] {
            "What do you want?",
            "Why are you talking to me?",
            "Sigh"
        };
        private string[] _normal = new string[] {
            "Hello?",
            "Hi?",
            "W-What?"
        };
        private string[] _love = new string[] {
            "Hello :3",
            "Yes, that's me <3",
            "<3"
        };
        private Random _random;

        public AsunaPlugin(IIrcClient client)
            : base(client)
        {
            _status = "Asuna plugin loaded";
            _random = new Random();
        }

        public override string Name
        {
            get { return "Asuna Plugin"; }
        }

        public override string Status
        {
            get { return _status; }
        }

        public override IList<string> Buttons
        {
            get { return new string[] { "About" }; }
        }

        protected override void ChannelMessageRecieved(object sender, ChatSharp.Events.PrivateMessageEventArgs e)
        {
            _status = string.Format("{0}: {1} ({2})",
                e.PrivateMessage.Source,
                e.PrivateMessage.Message.ToLower(),
                e.PrivateMessage.Message.ToLower().Contains("asuna"));
            this.SendPropertyChanged("Status");
            if (e.PrivateMessage.Message.ToLower().Contains("asuna"))
            {
                var message = "";
                if ((new string[] {"heyman", "jakeman95"}).Contains(e.PrivateMessage.User.Nick.ToLower()))
                {
                    message = _mean[_random.Next(0, _mean.Length - 1)];
                }
                else if ((new string[] { "altazure", "xmythycle", "thething|24-7", "thething", "thething|phone" }).Contains(e.PrivateMessage.User.Nick.ToLower()))
                {
                    message = _love[_random.Next(0, _love.Length - 1)];
                }
                else
                {
                    message = _normal[_random.Next(0, _normal.Length - 1)];
                }
                _client.Client.SendMessage(message, e.PrivateMessage.Source);
            }
        }

        public override void Open(string name)
        {
            if (_window != null)
                return;
            About w = new About();
            OpenWindow(w);
        }
    }
}

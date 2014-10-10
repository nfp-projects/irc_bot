using System;
using System.Collections.Generic;
using System.Linq;
using IRCBot.Bot;
using System.Text;
using System.Threading.Tasks;

namespace AsunaPlugin
{
    public class AsunaPlugin : BotPlugin
    {
        private string _status;

        public AsunaPlugin()
            : base()
        {
            _status = "Asuna plugin loaded";
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
                _client.Client.SendMessage("That's me \\o/", e.PrivateMessage.Source);
            }
        }
    }
}

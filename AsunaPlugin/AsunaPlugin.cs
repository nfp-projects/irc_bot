using System;
using System.Collections.Generic;
using System.Linq;
using IRCPlugin;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsunaPlugin
{
    public class AsunaPlugin : BotPlugin
    {
        private Dictionary<string, int> _mood;
        private Dictionary<string, DateTime> _timers;
        private Timer _t;
        private List<Action> _actions;
        private string _status;

        public AsunaPlugin(IIrcClient client)
            : base(client)
        {
            _status = "Asuna plugin loaded";
            _actions = new List<Action>();
            _mood = new Dictionary<string, int>();
            _timers = new Dictionary<string, DateTime>();
          
            _actions.Add(new Action(this, "hello", new int[] { -5, 0, 5 },
                new string[] { "Please stop talking to me", "Can you leave me alone?", "Sigh", "Urusai!", "No!", "I hate you!" },
                new string[] { "Uhh, hi?", "H-Hi?", "Yeah?", "Do you need something?"},
                new string[] { "Hi o/", "Hello!", "Hi!", "o/"},
                new string[] { "Hi %s ^_^", "Hi <3", "That's me <3", "%s <3"}));

            _t = new Timer(Timer_Ticked, null, 60000, 60 * 1000);
        }

        protected void Timer_Ticked(object state)
        {
            _status = string.Format("Timer ticked. Parsing {0} users.", _timers.Keys.Count);
            SendPropertyChanged("Status");

            lock (_timers)
            {
                var keys = new List<string>(_timers.Keys);
                for (int i = 0; i < keys.Count; i++)
                {
                    var delta = (DateTime.Now - _timers[keys[i]]);
                    if (delta.TotalMinutes > 5 && _mood[keys[i]] < 0)
                    {
                        _mood[keys[i]]++;
                        _timers[keys[i]] = DateTime.Now;
                    }
                    else if (delta.TotalDays > 1 && _mood[keys[i]] > 0)
                    {
                        _mood[keys[i]]--;
                        _timers[keys[i]] = DateTime.Now;
                    }
                }
            }
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
            if (!_mood.ContainsKey(e.PrivateMessage.User.Nick))
                _mood.Add(e.PrivateMessage.User.Nick, 0);

            string message = e.PrivateMessage.Message.ToLower();

            if (message == "$affection")
            {
                _client.Client.SendMessage(String.Format("« your affection is at {1}{0} »", _mood[e.PrivateMessage.User.Nick], _mood[e.PrivateMessage.User.Nick] >= 0 ? "+" : ""), e.PrivateMessage.Source);
                return;
            }


            if (!message.Contains("asuna-chan"))
            {
                _status = string.Format("No Asuna in '{0}', ignoring", message);
                SendPropertyChanged("Status");
                return;
            }

            _status = "Asuna-chan found! ";

            int mood = _mood[e.PrivateMessage.User.Nick];

            int change = 0;
            for (int i = 0; i < _actions.Count; i++)
            {
                string outcome = _actions[i].Parse(e.PrivateMessage.User.Nick, message, mood, out change);
                if (string.IsNullOrEmpty(outcome))
                    continue;

                _status += string.Format("Handler found {0}, sending: {1}", _actions[i].ActionName, outcome);

                lock (_timers)
                {
                    if (_timers.ContainsKey(e.PrivateMessage.User.Nick))
                        _timers.Remove(e.PrivateMessage.User.Nick);
                    _timers.Add(e.PrivateMessage.User.Nick, DateTime.Now);
                }

                outcome.Replace("%s", e.PrivateMessage.User.Nick);

                //Update mood
                _mood[e.PrivateMessage.User.Nick] += change;

                //Get diff
                int diff = _mood[e.PrivateMessage.User.Nick] - mood;

                //Send message
                _client.Client.SendMessage(String.Format("{0} (affection {2}{1})", outcome, diff, diff >= 0 ? "+" : ""), e.PrivateMessage.Source);
                SendPropertyChanged("Status");
                return;
            }
            _status += "No handlers found. Ignoring";
            SendPropertyChanged("Status");
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

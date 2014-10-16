using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsunaPlugin
{
    public class Action
    {
        private List<string[]> _reactions;
        private List<int> _indexes;
        private Dictionary<string, DateTime> _timers;
        private Timer _t;
        private int[] _moods;
        private string _action;
        private Random _random;

        public Action(AsunaPlugin asuna, string action, int[] moods, params string[][] reactions)
        {
            _action = action;
            _random = new Random();
            _reactions = new List<string[]>();
            _indexes = new List<int>();
            _timers = new Dictionary<string, DateTime>();
            _t = new Timer(Timer_Ticked, null, 60000, 60 * 1000);
            _moods = moods;

            for (int i = 0; i < reactions.Length; i++)
            {
                _reactions.Add(reactions[i].OrderBy(x => _random.Next()).ToArray());
                _indexes.Add(0);
            }
        }

        protected void Timer_Ticked(object state)
        {
            lock (_timers)
            {
                var keys = new List<string>(_timers.Keys);
                for (int i = 0; i < keys.Count; i++)
                {
                    var delta = (DateTime.Now - _timers[keys[i]]);
                    if (delta.TotalMinutes > 15)
                    {
                        _timers.Remove(keys[i]);
                    }
                }
            }
        }

        public virtual void Randomise(int index)
        {
            _indexes[index] = 0;
            _reactions[index] = _reactions[index].OrderBy(x => _random.Next()).ToArray();
        }

        public virtual int GetIndex(int mood)
        {
            int i = 0;
            Console.WriteLine("Mood: {0}", mood);
            for (; i < _moods.Length; i++)
            {
                if (mood < _moods[i])
                    break;
                if (i > 0 && mood >= _moods[i - 1] && mood < _moods[i])
                    break;
            }
            return i;
        }

        public virtual string Parse(string nick, string message, int mood, out int change)
        {
            change = 1;

            if (!Helper.IsMatch(message, _action))
                return null;

            lock (_timers)
            {
                if (_timers.ContainsKey(nick))
                {
                    change = -2;
                    _timers[nick] = DateTime.Now;
                }
                else
                    _timers.Add(nick, DateTime.Now);
            }

            mood += change;

            int index = GetIndex(mood);
            string outcome = _reactions[index][_indexes[index]++];
            if (_indexes[index] == _reactions[index].Length)
                Randomise(index);
            return outcome;
        }

        public virtual List<string[]> Reactions
        {
            get { return _reactions; }
        }

        public virtual int[] Moods
        {
            get { return _moods; }
        }
        public string ActionName
        {
            get { return _action; }
        }
    }
}

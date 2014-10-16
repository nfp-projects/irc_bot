using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsunaPlugin
{
    public class Action
    {
        private List<string[]> _reactions;
        private List<int> _indexes;
        private int[] _moods;
        private string _action;
        private Random _random;

        public Action(AsunaPlugin asuna, string action, int[] moods, params string[][] reactions)
        {
            _action = action;
            _random = new Random();
            _reactions = new List<string[]>();
            _indexes = new List<int>();
            _moods = moods;

            for (int i = 0; i < reactions.Length; i++)
            {
                _reactions.Add(reactions[i].OrderBy(x => _random.Next()).ToArray());
                _indexes.Add(0);
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
            change = 0;

            if (!Helper.IsMatch(message, _action))
                return null;

            change = -1;

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

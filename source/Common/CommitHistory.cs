using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class CommitHistory
    {
        private Dictionary<int, Dictionary<int, Commit>> _history;

        public CommitHistory()
        {
            _history = new Dictionary<int, Dictionary<int, Commit>>();
        }

        public Dictionary<int, Commit> this[int i]
        {
            get
            {
                if (!_history.ContainsKey(i))
                    _history[i] = new Dictionary<int, Commit>();
                return _history[i];
            }
            set
            {
                _history[i] = value;
            }
        }
    }
    public class Commit
    {
        private int _generation;
        private int _value;

        public Commit(int generation, int value)
        {
            _generation = generation;
            _value = value;
        }

        public int Generation { get => _generation; set => _generation = value; }
        public int Value { get => _value; set => _value = value; }
    }
}

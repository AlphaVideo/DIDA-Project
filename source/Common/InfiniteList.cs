using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class InfiniteList<T> : List<T>
    {

        T default_item;

        public InfiniteList(T default_item) : base()
        {
            this.default_item = default_item;
        }

        public T GetItem(int index)
        {
            while (Count <= index) { this.Add(default_item); }
            return this[index];
        }

        public void SetItem(int index, T value)
        {
            while (Count <= index) { this.Add(default_item); }
            this[index] = value;
        }
    }
}

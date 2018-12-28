using System;
using System.Collections.Generic;
using System.Text;

namespace RC.DataPress.Collections
{
    class ListWithKeySet<TKey, T, TBase> : List<T> where T : TBase
    {
        Dictionary<TKey, T> _dict;
        public ListWithKeySet()
        {
            _dict = new Dictionary<TKey, T>();
        }

        public ListWithKeySet(int initialCapacity) : base(initialCapacity)
        {
            _dict = new Dictionary<TKey, T>();
        }

        public bool TryGet(TKey id, ref TBase item)
        {
            if(_dict.TryGetValue(id, out var value))
            {
                item = value;
                return true;
            }
            return false;
        }

        public void Add(TKey id, T item)
        {
            Add(item);
            _dict.Add(id, item);
        }

        public bool TryAddNew(TKey id, ref T item, Func<T> itemFactory)
        {
            if (_dict.TryGetValue(id, out item))
                return false; // already exists

            item = itemFactory();

            Add(id, item);

            return true;
        }

        public bool TryAddNew(TKey id, int descriminator, ref T item, Func<int, T> itemFactory)
        {
            return TryAddNew(id, ref item, () => itemFactory(descriminator));
        }

        public bool TryAddNew(TKey id, ref TBase item, Dictionary<TKey, TBase> dict, Func<T> itemFactory)
        {
            if (TryGet(id, ref item))
                return false; // already exists

            if (dict.TryGetValue(id, out var d))
            {
                item = d;
            }
            else
            {
                item = itemFactory();
                dict.Add(id, item);
            }

            Add((T)item);
            return true;
        }

        public bool TryAddNew(TKey id, int descriminator, ref TBase item, Dictionary<TKey, TBase> dict, Func<int, T> itemFactory)
        {
            if (TryGet(id, ref item))
                return false; // already exists

            if (dict.TryGetValue(id, out var d))
            {
                item = d;
            }
            else
            {
                item = itemFactory(descriminator);
                dict.Add(id, (T)item);
            }

            Add((T)item);
            return true;
        }
    }
}

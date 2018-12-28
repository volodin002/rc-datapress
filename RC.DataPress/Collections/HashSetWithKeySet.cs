using System;
using System.Collections.Generic;
using System.Text;

namespace RC.DataPress.Collections
{
    class HashSetWithKeySet<TKey, T, TBase> : HashSet<T> where T : TBase
    {
        Dictionary<TKey, T> _dict;
        public HashSetWithKeySet()
        {
            _dict = new Dictionary<TKey, T>();
        }

        public bool TryGet(TKey id, ref TBase item)
        {
            if (_dict.TryGetValue(id, out var value))
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

            Add(item);
            _dict.Add(id, item);

            return true;
        }

        public bool TryAddNew(TKey id, int descriminator, ref T item, Func<int, T> itemFactory)
        {
            return TryAddNew(id, ref item, () => itemFactory(descriminator));
        }
    }
}

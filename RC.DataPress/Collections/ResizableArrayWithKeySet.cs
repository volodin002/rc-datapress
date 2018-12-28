using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace RC.DataPress.Collections
{
    public class ResizableArrayWithKeySet<TKey, T, TBase> : IList<T>, ICollection<T> where T : TBase
    {
        // store lower 31 bits of hash code
        protected const int Lower31BitMask = 0x7FFFFFFF;

        public struct Slot
        {
            //internal int hashCode;      // Lower 31 bits of hash code, -1 if unused
            internal int next;          // Index of next entry, -1 if last
            internal TKey key;
            internal T value;
        }

        protected int[] _hash_array;
        protected Slot[] _array;
        protected int _count;

        public ResizableArrayWithKeySet()
        {
        }
        public ResizableArrayWithKeySet(int initialCapacity) 
        {
            _hash_array = new int[initialCapacity];
            _array = new Slot[initialCapacity];
        }

        public bool TryGet(TKey id, ref TBase item)
        {
            var comparer = EqualityComparer<TKey>.Default;
            int hashCode = comparer.GetHashCode(id) & Lower31BitMask; // get hash without sign bit
            int length = _array.Length;

            int hashIndex = hashCode % length;

            for (int i = _hash_array[hashIndex] - 1; i >= 0; i = _array[i].next)
            {
                if (comparer.Equals(_array[i].key, id))
                {
                    item = _array[i].value;
                    return true; 
                }
            }

            return false;
        }

        public bool TryAddNew(TKey id, ref T item, Func<T> itemFactory)
        {
            var comparer = EqualityComparer<TKey>.Default;
            int hashCode = comparer.GetHashCode(id) & Lower31BitMask;
            int length = _array.Length;

            int hashIndex = hashCode % length;

            for (int i = _hash_array[hashIndex] - 1; i >= 0; i = _array[i].next)
            {
                if (comparer.Equals(_array[i].key, id))
                {
                    item = _array[i].value;
                    return false; // already exists !
                }
            }

            if (_count == length) // resize buffers
            {
                length = length == 0 ? 4 : length * 2;
                Array.Resize(ref _array, length);

                _hash_array = new int[length];

                for (int i = 0; i < length; i++)
                {
                    int idHashCode = comparer.GetHashCode(_array[i].key) & Lower31BitMask;

                    int idHashIndex = idHashCode % length;
                    _array[i].next = _hash_array[idHashIndex] - 1;
                    _hash_array[idHashIndex] = i + 1;
                }
                // recompute hashIndex with new array Length
                hashIndex = hashCode % length;
            }

            item = itemFactory();

            _array[_count++] = new Slot() { key = id, value = item, next = _hash_array[hashIndex] - 1 };
            _hash_array[hashIndex] = _count;

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

            Add(id, (T)item);
            return true;
        }

        public bool TryAddNew(TKey id, int descriminator, ref TBase item, Dictionary<TKey, TBase> dict, Func<int, T> itemFactory)
        {
            if (TryGet(id, ref item))
                return false; // already exists

            if (dict.TryGetValue(id, out var d))
            {
                item = (T)d;
            }
            else
            {
                item = itemFactory(descriminator);
                dict.Add(id, item);
            }

            Add(id, (T)item);
            return true;
        }

        public void Add(TKey id, T item)
        {
            var comparer = EqualityComparer<TKey>.Default;
            int hashCode = comparer.GetHashCode(id) & Lower31BitMask;
            int length = _array.Length;

            int hashIndex = hashCode % length;

            if (_count == length) // resize buffers
            {
                length = length == 0 ? 4 : length * 2;
                Array.Resize(ref _array, length);

                _hash_array = new int[length];

                for (int i = 0; i < length; i++)
                {
                    int idHashCode = comparer.GetHashCode(_array[i].key) & Lower31BitMask;

                    int idHashIndex = idHashCode % length;
                    _array[i].next = _hash_array[idHashIndex] - 1;
                    _hash_array[idHashIndex] = i + 1;
                }
                // recompute hashIndex with new array Length
                hashIndex = hashCode % length;
            }

            _array[_count++] = new Slot() { value = item, next = _hash_array[hashIndex] - 1 };
            _hash_array[hashIndex] = _count;
        }

        public Slot[] InternalArray { get { return _array; } }

        #region ICollection
        public int Count => _count;

        public bool IsReadOnly => false;

        public void Add(T element)
        {
            int length = _array.Length;
            if (_count == length)
            {
                length = length == 0 ? 4 : length * 2;
                Array.Resize(ref _array, length);
            }

            _array[_count++] = new Slot() { value = element, next = -1 };
        }

        public void Clear()
        {
            _count = 0; _array = null;
        }

        public bool Contains(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < _count; i++)
            {
                if (comparer.Equals(item, _array[i].value)) return true;
            }

            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < _count; i++)
            {
                array[i + arrayIndex] = _array[i].value;
            }
        }

        #endregion // ICollection

        #region IList

        public T this[int index] { get => _array[index].value; set => _array[index].value = value; }

        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < _count; i++)
            {
                if (comparer.Equals(item, _array[i].value)) return i;
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            int length = _array.Length;
            if (++_count == length)
            {
                if (length == 0)
                    Array.Resize(ref _array, 4);
                else
                    Array.Resize(ref _array, length * 2);
            }
            for (int i = index; i < _count; i++)
            {
                _array[i + 1] = _array[i];
            }
            _array[index] = new Slot() { value = item, next = -1 };
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index < 0) return false;

            RemoveAt(index);

            return true;
        }

        public void RemoveAt(int index)
        {
            _count--;
            for (int i = index; i < _count; i++)
            {
                _array[i] = _array[i + 1];
            }
        }

        #endregion // IList

        #region IEnumerable

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(_array, _count);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(_array, _count);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private Slot[] _array;
            private int _index;
            private int _count;
            private T _current;

            internal Enumerator(Slot[] array, int count)
            {
                _array = array;
                _index = 0;
                _count = count;
                _current = default(T);
            }

            public void Dispose()
            {
                _array = null;
                _current = default(T);
            }

            public bool MoveNext()
            {
                if (_index < _count)
                {
                    _current = _array[_index++].value;
                    return true;
                }

                _current = default(T);
                return false;
            }

            public void Reset()
            {
                _index = 0;
                _current = default(T);
            }

            public T Current => _current;

            object IEnumerator.Current => throw new NotImplementedException();
        }

        #endregion // IEnumerable
    }
}

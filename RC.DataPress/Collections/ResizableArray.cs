using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RC.DataPress.Collections
{
    public class ResizableArray<T> : IList<T>, ICollection<T> //where T: class
    {
        //static T[] _empty = new T[0];
        T[] _array;
        int _count;

        public ResizableArray()
        {
            //_array = _empty;
        }
        public ResizableArray(int initialCapacity)
        {
            _array = new T[initialCapacity]; 
        }

        public ResizableArray(T[] array)
        {
            _array = array;
            _count = array.Length;

        }

        public T this[int index] { get => _array[index]; set => _array[index] = value; }

        public T[] InternalArray { get { return _array; } }

        public int Count => _count;

        public bool IsReadOnly => false;

        public void Add(T element)
        {
            int length = _array.Length;
            if (_count == length)
            {
                length = length == 0 ? 4 : length * 2;
                Array.Resize(ref _array, length);

                //if (length == 0)
                //    Array.Resize(ref _array, 4);
                //else
                //    Array.Resize(ref _array, length * 2);
            }

            _array[_count++] = element;
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
                if (comparer.Equals(item,_array[i])) return true;
            }

            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Array.Copy(_array, 0, array, arrayIndex, _count);
        }

        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < _count; i++)
            {
                if (comparer.Equals(item, _array[i])) return i;
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            int length = _array.Length;
            if (++_count == length)
            {
                length = length == 0 ? 4 : length * 2;
                Array.Resize(ref _array, length);

                //if (length == 0)
                //    Array.Resize(ref _array, 4);
                //else
                //    Array.Resize(ref _array, length * 2);
            }
            for (int i = index; i < _count; i++)
            {
                _array[i + 1] = _array[i];
            }
            _array[index] = item;
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
            private T[] _array;
            private int _index;
            private int _count;
            private T _current;

            internal Enumerator(T[] array, int count)
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
                    _current = _array[_index++];
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

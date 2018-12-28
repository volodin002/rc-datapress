using System;
using System.Collections.Generic;
using System.Text;

namespace RC.DataPress.Collections
{
    /*
    class HashSetX<T>
    {
        internal struct Slot
        {
            internal int hashCode;      // Lower 31 bits of hash code, -1 if unused
            internal int next;          // Index of next entry, -1 if last
            internal T value;
        }

        private int[] m_buckets;
        private Slot[] m_slots;

        private int InternalIndexOf(T item)
        {
            //EqualityComparer<T>.Default

            int hashCode = InternalGetHashCode(item);
            for (int i = m_buckets[hashCode % m_buckets.Length] - 1; i >= 0; i = m_slots[i].next)
            {
                if ((m_slots[i].hashCode) == hashCode && m_comparer.Equals(m_slots[i].value, item))
                {
                    return i;
                }
            }
            // wasn't found
            return -1;
        }

        private void SetCapacity(int newSize, bool forceNewHashCodes)
        {
            Slot[] newSlots = new Slot[newSize];
            if (m_slots != null)
            {
                Array.Copy(m_slots, 0, newSlots, 0, m_lastIndex);
            }

            if (forceNewHashCodes)
            {
                for (int i = 0; i < m_lastIndex; i++)
                {
                    if (newSlots[i].hashCode != -1)
                    {
                        newSlots[i].hashCode = InternalGetHashCode(newSlots[i].value);
                    }
                }
            }

            int[] newBuckets = new int[newSize];
            for (int i = 0; i < m_lastIndex; i++)
            {
                int bucket = newSlots[i].hashCode % newSize;
                newSlots[i].next = newBuckets[bucket] - 1;
                newBuckets[bucket] = i + 1;
            }
            m_slots = newSlots;
            m_buckets = newBuckets;
        }

        private bool AddIfNotPresent(T value)
        {
            if (m_buckets == null)
            {
                Initialize(0);
            }

            int hashCode = InternalGetHashCode(value);
            int bucket = hashCode % m_buckets.Length;

            for (int i = m_buckets[hashCode % m_buckets.Length] - 1; i >= 0; i = m_slots[i].next)
            {
                if (m_slots[i].hashCode == hashCode && m_comparer.Equals(m_slots[i].value, value))
                {
                    return false;
                }

            }

            int index;
            if (m_freeList >= 0)
            {
                index = m_freeList;
                m_freeList = m_slots[index].next;
            }
            else
            {
                if (m_lastIndex == m_slots.Length)
                {
                    IncreaseCapacity();
                    // this will change during resize
                    bucket = hashCode % m_buckets.Length;
                }
                index = m_lastIndex;
                m_lastIndex++;
            }
            m_slots[index].hashCode = hashCode;
            m_slots[index].value = value;
            m_slots[index].next = m_buckets[bucket] - 1;
            m_buckets[bucket] = index + 1;
            m_count++;
            m_version++;


            return true;
        }

        /// <summary>
        /// Searches the set for a given value and returns the equal value it finds, if any.
        /// </summary>
        /// <param name="equalValue">The value to search for.</param>
        /// <param name="actualValue">The value from the set that the search found, or the default value of <typeparamref name="T"/> when the search yielded no match.</param>
        /// <returns>A value indicating whether the search was successful.</returns>
        /// <remarks>
        /// This can be useful when you want to reuse a previously stored reference instead of 
        /// a newly constructed one (so that more sharing of references can occur) or to look up
        /// a value that has more complete data than the value you currently have, although their
        /// comparer functions indicate they are equal.
        /// </remarks>
        public bool TryGetValue(T equalValue, out T actualValue)
        {
            if (m_buckets != null)
            {
                int i = InternalIndexOf(equalValue);
                if (i >= 0)
                {
                    actualValue = m_slots[i].value;
                    return true;
                }
            }
            actualValue = default(T);
            return false;
        }

        public bool Remove(T item)
        {
            if (m_buckets != null)
            {
                int hashCode = InternalGetHashCode(item);
                int bucket = hashCode % m_buckets.Length;
                int last = -1;
                for (int i = m_buckets[bucket] - 1; i >= 0; last = i, i = m_slots[i].next)
                {
                    if (m_slots[i].hashCode == hashCode && m_comparer.Equals(m_slots[i].value, item))
                    {
                        if (last < 0)
                        {
                            // first iteration; update buckets
                            m_buckets[bucket] = m_slots[i].next + 1;
                        }
                        else
                        {
                            // subsequent iterations; update 'next' pointers
                            m_slots[last].next = m_slots[i].next;
                        }
                        m_slots[i].hashCode = -1;
                        m_slots[i].value = default(T);
                        m_slots[i].next = m_freeList;

                        m_count--;
                        m_version++;
                        if (m_count == 0)
                        {
                            m_lastIndex = 0;
                            m_freeList = -1;
                        }
                        else
                        {
                            m_freeList = i;
                        }
                        return true;
                    }
                }
            }
            // either m_buckets is null or wasn't found
            return false;
        }

        public struct Enumerator : IEnumerator<T>, System.Collections.IEnumerator
        {
            private HashSet<T> set;
            private int index;
            private int version;
            private T current;

            internal Enumerator(HashSet<T> set)
            {
                this.set = set;
                index = 0;
                version = set.m_version;
                current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (version != set.m_version)
                {
                    throw new InvalidOperationException(SR.GetString(SR.InvalidOperation_EnumFailedVersion));
                }

                while (index < set.m_lastIndex)
                {
                    if (set.m_slots[index].hashCode >= 0)
                    {
                        current = set.m_slots[index].value;
                        index++;
                        return true;
                    }
                    index++;
                }
                index = set.m_lastIndex + 1;
                current = default(T);
                return false;
            }
        }
    }
    */
}

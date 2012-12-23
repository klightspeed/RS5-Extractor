using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public class SubArray<T> : IList<T>
    {
        protected IList<T> ParentData;
        protected int DataOffset;
        protected int DataLength;

        public SubArray(IList<T> data, int offset, int length)
        {
            this.ParentData = data;
            this.DataOffset = offset;
            this.DataLength = length;
        }

        public SubArray(IList<T> data)
            : this(data, 0, data.Count)
        {
        }

        public int Count
        {
            get
            {
                return DataLength;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= DataLength)
                {
                    throw new IndexOutOfRangeException();
                }
                return ParentData[DataOffset + index];
            }
            set
            {
                throw new InvalidOperationException();
            }
        }

        public void Add(T item)
        {
            throw new InvalidOperationException();
        }

        public void Clear()
        {
            throw new InvalidOperationException();
        }

        public bool Contains(T item)
        {
            foreach (T v in this)
            {
                if (item.Equals(v))
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i < DataLength; i++)
            {
                array[arrayIndex + i] = ParentData[DataOffset + i];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = DataOffset; i < DataOffset + DataLength; i++)
            {
                yield return ParentData[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(T item)
        {
            for (int i = DataOffset; i < DataOffset + DataLength; i++)
            {
                if (item.Equals(ParentData[i]))
                {
                    return i - DataOffset;
                }
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
            throw new InvalidOperationException();
        }

        public bool Remove(T item)
        {
            throw new InvalidOperationException();
        }

        public void RemoveAt(int index)
        {
            throw new InvalidOperationException();
        }
    }

    public class ByteSubArray : SubArray<byte>
    {
        public int Position;

        public ByteSubArray(IList<byte> data, int offset, int length)
            : base(data, offset, length)
        {
        }

        public ByteSubArray(IList<byte> data)
            : base(data)
        {
        }

        public byte[] GetBytes(int offset, int length)
        {
            byte[] data = new byte[length];
            for (int i = 0; i < length; i++)
            {
                data[i] = this[i + offset];
            }
            return data;
        }

        public int GetInt32(int offset)
        {
            return BitConverter.ToInt32(GetBytes(offset, 4), 0);
        }

        public int GetInt32()
        {
            int pos = Position;
            Position += 4;
            return GetInt32(pos);
        }

        public long GetInt64(int offset)
        {
            return BitConverter.ToInt64(GetBytes(offset, 8), 0);
        }

        public long GetInt64()
        {
            int pos = Position;
            Position += 8;
            return GetInt64(pos);
        }

        public float GetSingle(int offset)
        {
            return BitConverter.ToSingle(GetBytes(offset, 4), 0);
        }

        public float GetSingle()
        {
            int pos = Position;
            Position += 4;
            return GetSingle(pos);
        }

        protected string GetString(int offset, int length, out int endoffset)
        {
            StringBuilder sb = new StringBuilder();
            int pos = offset;
            for (pos = offset; pos < offset + length && this[pos] != 0; pos++)
            {
                sb.Append((char)this[pos]);
            }
            pos++;
            endoffset = pos;
            
            return sb.ToString();
        }
        
        public string GetString(int offset, int length)
        {
            int endofs;
            return GetString(offset, length, out endofs);
        }

        public string GetString(int offset)
        {
            return GetString(offset, this.Count - offset);
        }

        public string GetString()
        {
            return GetString(Position, this.Count - Position, out Position);
        }

        public byte GetByte()
        {
            int pos = Position;
            Position++;
            return this[pos];
        }
    }
}

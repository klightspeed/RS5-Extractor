using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using Ionic.Zlib;

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

        protected SubArray()
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
        private static Dictionary<Type, Func<ByteSubArray, int, dynamic>> Getters = new Dictionary<Type, Func<ByteSubArray, int, dynamic>>();
        public int Position;

        static ByteSubArray()
        {
            Getters[typeof(byte)] = (a, o) => a.GetByte(o);
            Getters[typeof(sbyte)] = (a, o) => a.GetSByte(o);
            Getters[typeof(short)] = (a, o) => a.GetInt16(o);
            Getters[typeof(ushort)] = (a, o) => a.GetUInt16(o);
            Getters[typeof(int)] = (a, o) => a.GetInt32(o);
            Getters[typeof(uint)] = (a, o) => a.GetUInt32(o);
            Getters[typeof(long)] = (a, o) => a.GetInt64(o);
            Getters[typeof(ulong)] = (a, o) => a.GetUInt64(o);
            Getters[typeof(float)] = (a, o) => a.GetSingle(o);
            Getters[typeof(double)] = (a, o) => a.GetDouble(o);
        }

        public ByteSubArray(IList<byte> data, int offset, int length)
            : base(data, offset, length)
        {
        }

        public ByteSubArray(IList<byte> data)
            : base(data)
        {
        }

        protected ByteSubArray()
            : base()
        {
        }

        private void InitGetters()
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

        public byte   GetByte(int offset)   { return this[offset]; }
        public sbyte  GetSByte(int offset)  { return unchecked((sbyte)this[offset]); }
        public short  GetInt16(int offset)  { return BitConverter.ToInt16(GetBytes(offset, 2), 0); }
        public ushort GetUInt16(int offset) { return BitConverter.ToUInt16(GetBytes(offset, 2), 0); }
        public int    GetInt32(int offset)  { return BitConverter.ToInt32(GetBytes(offset, 4), 0); }
        public uint   GetUInt32(int offset) { return BitConverter.ToUInt32(GetBytes(offset, 4), 0); }
        public long   GetInt64(int offset)  { return BitConverter.ToInt64(GetBytes(offset, 8), 0); }
        public ulong  GetUInt64(int offset) { return BitConverter.ToUInt64(GetBytes(offset, 8), 0); }
        public float  GetSingle(int offset) { return BitConverter.ToSingle(GetBytes(offset, 4), 0); }
        public double GetDouble(int offset) { return BitConverter.ToDouble(GetBytes(offset, 8), 0); }

        private dynamic InternalRead(Func<ByteSubArray, int, dynamic> getter, int size)
        {
            int pos = Position;
            Position += size;
            return getter(this, pos);
        }

        public byte   ReadByte()   { return InternalRead(Getters[typeof(byte)],   sizeof(byte)); }
        public sbyte  ReadSByte()  { return InternalRead(Getters[typeof(sbyte)],  sizeof(sbyte)); }
        public short  ReadInt16()  { return InternalRead(Getters[typeof(short)],  sizeof(short)); }
        public ushort ReadUInt16() { return InternalRead(Getters[typeof(ushort)], sizeof(ushort)); }
        public int    ReadInt32()  { return InternalRead(Getters[typeof(int)],    sizeof(int)); }
        public uint   ReadUInt32() { return InternalRead(Getters[typeof(uint)],   sizeof(uint)); }
        public long   ReadInt64()  { return InternalRead(Getters[typeof(long)],   sizeof(long)); }
        public ulong  ReadUInt64() { return InternalRead(Getters[typeof(ulong)],  sizeof(ulong)); }
        public float  ReadSingle() { return InternalRead(Getters[typeof(float)],  sizeof(float)); }
        public double ReadDouble() { return InternalRead(Getters[typeof(double)], sizeof(double)); }

        public Matrix4 GetMatrix4(int offset)
        {
            return new Matrix4(
                GetSingle(offset +  0), GetSingle(offset + 16), GetSingle(offset + 32), GetSingle(offset + 48),
                GetSingle(offset +  4), GetSingle(offset + 20), GetSingle(offset + 36), GetSingle(offset + 52),
                GetSingle(offset +  8), GetSingle(offset + 24), GetSingle(offset + 40), GetSingle(offset + 56),
                GetSingle(offset + 12), GetSingle(offset + 28), GetSingle(offset + 44), GetSingle(offset + 60)
            );
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

        public string ReadString()
        {
            return GetString(Position, this.Count - Position, out Position);
        }
    }

    public class CompressedByteSubArray : ByteSubArray
    {
        public readonly int TotalSize;
        public readonly int CompressedSize;

        public CompressedByteSubArray(Stream stream, long offset)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            using (ZlibStream zstream = new ZlibStream(stream, CompressionMode.Decompress, true))
            {
                using (MemoryStream outstrm = new MemoryStream())
                {
                    zstream.CopyTo(outstrm);
                    TotalSize = (int)zstream.TotalOut;
                    CompressedSize = (int)zstream.TotalIn;
                    this.ParentData = outstrm.ToArray();
                    this.Position = 0;
                    this.DataLength = TotalSize;
                }
            }
        }
    }
}

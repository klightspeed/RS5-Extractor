using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using Ionic.Zlib;
using System.Diagnostics;

namespace RS5_Extractor
{
    public class OffsettableStream : Stream
    {
        protected virtual Stream InternalStream { get; set; }
        protected long DataOffset;
        protected long DataLength;

        public OffsettableStream(Stream stream, long offset, long length)
        {
            if (!stream.CanRead || !stream.CanSeek)
            {
                throw new ArgumentException("stream must be readable and seekable");
            }

            this.InternalStream = stream;
            this.DataOffset = offset;
            this.DataLength = length;
        }

        public OffsettableStream(Stream stream)
            : this(stream, 0, stream.Length)
        {
        }

        protected OffsettableStream()
        {
        }

        public override bool CanRead { get { return InternalStream.CanRead; } }
        public override bool CanSeek { get { return InternalStream.CanSeek; } }
        public override bool CanTimeout { get { return InternalStream.CanTimeout; } }
        public override bool CanWrite { get { return InternalStream.CanWrite; } }
        public override long Length { get { return Math.Min(InternalStream.Length - DataOffset, DataLength); } }
        public override long Position { get { return InternalStream.Position - DataOffset; } set { InternalStream.Position = value + DataOffset; } }
        public override int ReadTimeout { get { return InternalStream.ReadTimeout; } set { InternalStream.ReadTimeout = value; } }
        public override int WriteTimeout { get { return InternalStream.WriteTimeout; } set { InternalStream.WriteTimeout = value; } }
        public override void Close() { InternalStream.Close(); }
        public override void Flush() { InternalStream.Flush(); }
        public override int Read(byte[] buffer, int offset, int count) { return InternalStream.Read(buffer, offset, count); }
        public override void SetLength(long value) { throw new InvalidOperationException(); }
        public override void Write(byte[] buffer, int offset, int count) { InternalStream.Write(buffer, offset, count); }

        public override long Seek(long offset, SeekOrigin origin) 
        {
            switch (origin)
            {
                case SeekOrigin.Begin: return InternalStream.Seek(offset + DataOffset, SeekOrigin.Begin);
                case SeekOrigin.Current: return InternalStream.Seek(offset, SeekOrigin.Current);
                case SeekOrigin.End: return InternalStream.Seek(DataOffset + DataLength - offset, SeekOrigin.Begin);
                default: throw new InvalidOperationException();
            }
        }

        public virtual int ReadAt(byte[] buffer, int offset, int count, long position)
        {
            if (Position != position) Seek(position, SeekOrigin.Begin);
            return Read(buffer, offset, count);
        }

        public virtual void WriteAt(byte[] buffer, int offset, int count, long position)
        {
            if (Position != position) Seek(position, SeekOrigin.Begin);
            Write(buffer, offset, count);
        }
    }

    public class OffsettableMemoryStream : OffsettableStream
    {
        protected const int blocksize = 32768;
        protected List<byte[]> _InternalBuffers;
        protected long _Position;
        protected long _Length;

        public override long Position { get { return _Position; } set { _Position = value; } }
        public override long Length { get { return _Length; } }

        public override void SetLength(long value)
        {
            if (value >= Length)
            {
                while ((value + blocksize - 1) / blocksize > _InternalBuffers.Count)
                {
                    _InternalBuffers.Add(new byte[blocksize]);
                }
            }
            else
            {
                int newnrblocks = (int)((value + blocksize - 1) / blocksize);
                _InternalBuffers.RemoveRange(newnrblocks, _InternalBuffers.Count - newnrblocks);
            }

            _Length = value;
        }

        public OffsettableMemoryStream(int capacity)
        {
            this._InternalBuffers = Enumerable.Range(0, (capacity + blocksize - 1) / blocksize).Select(i => new byte[blocksize]).ToList();
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return true; } }
        public override bool CanTimeout { get { return false; } }
        public override bool CanWrite { get { return true; } }
        public override int ReadTimeout { get { return 0; } set { } }
        public override int WriteTimeout { get { return 0; } set { } }
        public override void Close() { }
        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) 
        {
            count = ReadAt(buffer, offset, count, Position);
            Position += count;
            return count;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAt(buffer, offset, count, Position);
            Position += count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: Position = offset; break;
                case SeekOrigin.Current: Position += offset; break;
                case SeekOrigin.End: Position = Length - offset; break;
                default: throw new InvalidOperationException();
            }

            return Position;
        }

        public override int ReadAt(byte[] buffer, int offset, int count, long position)
        {
            if (position >= Length)
            {
                return 0;
            }

            if (position + count > Length)
            {
                count = (int)(Length - position);
            }

            int blocknr = (int)(position / blocksize);
            int blockpos = (int)(position % blocksize);
            int remain = count;

            while (remain > 0)
            {
                int copylen = Math.Min(remain, blocksize - blockpos);
                Array.Copy(_InternalBuffers[blocknr], blockpos, buffer, offset, copylen);
                offset += copylen;
                remain -= copylen;
                blockpos = 0;
                blocknr++;
            }

            return count;
        }

        public override void WriteAt(byte[] buffer, int offset, int count, long position)
        {
            if (position + count > Length)
            {
                SetLength(position + count);
            }

            int blocknr = (int)(position / blocksize);
            int blockpos = (int)(position % blocksize);
            int remain = count;

            while (remain > 0)
            {
                int copylen = Math.Min(remain, blocksize - blockpos);
                Array.Copy(buffer, offset, _InternalBuffers[blocknr], blockpos, copylen);
                offset += copylen;
                remain -= copylen;
                blockpos = 0;
                blocknr++;
            }
        }

        public void WriteTo(Stream stream)
        {
            int blocknr = 0;
            long remain = Length;

            while (remain > 0)
            {
                int copylen = (int)Math.Min(remain, blocksize);
                stream.Write(_InternalBuffers[blocknr], 0, copylen);
                remain -= copylen;
                blocknr++;
            }
        }
    }

    public class SubStream : OffsettableStream
    {
        protected OffsettableStream ParentStream { get { return (OffsettableStream)InternalStream; } }

        public SubStream(Stream data, long offset, long length)
            : base(data is OffsettableStream ? data : new OffsettableStream(data), offset, length)
        {
        }

        public SubStream(Stream data)
            : this(data, 0, data.Length)
        {
        }

        protected SubStream()
        {
        }

        public byte[] GetBytes(long offset, int length)
        {
            byte[] data = new byte[length];
            ReadAt(data, 0, length, offset);
            return data;
        }

        public void WriteBytes(int length, byte[] data)
        {
            Write(data, 0, length);
        }

        public void SetBytes(long offset, int length, byte[] data)
        {
            WriteAt(data, 0, length, offset);
        }

        public byte   GetByte  (long offset) { return GetBytes(offset, 1)[0]; }
        public sbyte  GetSByte (long offset) { return unchecked((sbyte)GetByte(offset)); }
        public short  GetInt16 (long offset) { return BitConverter.ToInt16(GetBytes(offset, 2), 0); }
        public ushort GetUInt16(long offset) { return BitConverter.ToUInt16(GetBytes(offset, 2), 0); }
        public int    GetInt32 (long offset) { return BitConverter.ToInt32(GetBytes(offset, 4), 0); }
        public uint   GetUInt32(long offset) { return BitConverter.ToUInt32(GetBytes(offset, 4), 0); }
        public long   GetInt64 (long offset) { return BitConverter.ToInt64(GetBytes(offset, 8), 0); }
        public ulong  GetUInt64(long offset) { return BitConverter.ToUInt64(GetBytes(offset, 8), 0); }
        public float  GetSingle(long offset) { return BitConverter.ToSingle(GetBytes(offset, 4), 0); }
        public double GetDouble(long offset) { return BitConverter.ToDouble(GetBytes(offset, 8), 0); }

        public byte[] ReadBytes(int length)
        {
            byte[] data = new byte[length];
            Read(data, 0, length);
            return data;
        }

        public sbyte  ReadSByte() { return unchecked((sbyte)ReadByte()); }
        public short  ReadInt16() { return BitConverter.ToInt16(ReadBytes(2), 0); }
        public ushort ReadUInt16() { return BitConverter.ToUInt16(ReadBytes(2), 0); }
        public int    ReadInt32() { return BitConverter.ToInt32(ReadBytes(4), 0); }
        public uint   ReadUInt32() { return BitConverter.ToUInt32(ReadBytes(4), 0); }
        public long   ReadInt64() { return BitConverter.ToInt64(ReadBytes(8), 0); }
        public ulong  ReadUInt64() { return BitConverter.ToUInt64(ReadBytes(8), 0); }
        public float  ReadSingle() { return BitConverter.ToSingle(ReadBytes(4), 0); }
        public double ReadDouble() { return BitConverter.ToDouble(ReadBytes(8), 0); }

        public void   SetByte(long offset, byte data) { SetBytes(offset, 1, new byte[] { data }); }
        public void   SetSByte (long offset, sbyte  data) { SetByte(offset, unchecked((byte)data)); }
        public void   SetInt16 (long offset, short  data) { SetBytes(offset, 2, BitConverter.GetBytes(data)); }
        public void   SetUInt16(long offset, ushort data) { SetBytes(offset, 2, BitConverter.GetBytes(data)); }
        public void   SetInt32 (long offset, int    data) { SetBytes(offset, 4, BitConverter.GetBytes(data)); }
        public void   SetUInt32(long offset, uint   data) { SetBytes(offset, 4, BitConverter.GetBytes(data)); }
        public void   SetInt64 (long offset, long   data) { SetBytes(offset, 8, BitConverter.GetBytes(data)); }
        public void   SetUInt64(long offset, ulong  data) { SetBytes(offset, 8, BitConverter.GetBytes(data)); }
        public void   SetSingle(long offset, float  data) { SetBytes(offset, 4, BitConverter.GetBytes(data)); }
        public void   SetDouble(long offset, double data) { SetBytes(offset, 8, BitConverter.GetBytes(data)); }

        public Matrix4 GetMatrix4(long offset)
        {
            return new Matrix4(
                GetSingle(offset +  0), GetSingle(offset + 16), GetSingle(offset + 32), GetSingle(offset + 48),
                GetSingle(offset +  4), GetSingle(offset + 20), GetSingle(offset + 36), GetSingle(offset + 52),
                GetSingle(offset +  8), GetSingle(offset + 24), GetSingle(offset + 40), GetSingle(offset + 56),
                GetSingle(offset + 12), GetSingle(offset + 28), GetSingle(offset + 44), GetSingle(offset + 60)
            );
        }

        protected string ReadString(int length, out int remain)
        {
            StringBuilder sb = new StringBuilder();

            while (length > 0)
            {
                int v = ReadByte();
                length--;
                if (v == 0 || v == -1) break;
                sb.Append((char)v);
            }

            remain = length;

            return sb.ToString();
        }

        protected string GetString(long offset, int length, out long endoffset)
        {
            Seek(offset, SeekOrigin.Begin);
            int remain;
            string ret = ReadString(length, out remain);
            endoffset = offset + length - remain;
            return ret;
        }
        
        public string GetString(long offset, int length)
        {
            long endofs;
            return GetString(offset, length, out endofs);
        }

        public string GetString(long offset)
        {
            return GetString(offset, (int)(this.Length - offset));
        }

        public string ReadString()
        {
            int remain;
            return ReadString(Int32.MaxValue, out remain);
        }
    }

    public class CompressedSubStream : SubStream
    {
        public int TotalSize { get; private set; }
        public int CompressedSize { get; private set; }

        protected OffsettableStream Decompress(Stream stream, long offset, int allocsize)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            using (ZlibStream zstream = new ZlibStream(stream, CompressionMode.Decompress, true))
            {
                OffsettableMemoryStream outstrm = new OffsettableMemoryStream(allocsize);
                zstream.CopyTo(outstrm);
                outstrm.Position = 0;
                TotalSize = (int)zstream.TotalOut;
                CompressedSize = (int)zstream.TotalIn;
                this.DataLength = TotalSize;
                return outstrm;
            }
        }

        public CompressedSubStream(Stream stream, long offset, int allocsize)
        {
            InternalStream = Decompress(stream, offset, allocsize);
        }
    }
}

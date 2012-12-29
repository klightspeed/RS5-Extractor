using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ionic.Zlib;

namespace RS5_Extractor
{
    public class RS5Chunk
    {

        #region Lazy-init property backing

        private ByteSubArray _ChunkData;
        private Lazy<ByteSubArray> _Data;
        private Lazy<Dictionary<string, RS5Chunk>> _Chunks;
        
        #endregion

        #region Lazy-init properties
        
        public ByteSubArray Data { get { return _Data.Value; } }
        public Dictionary<string, RS5Chunk> Chunks { get { return _Chunks.Value; } }
        public virtual ByteSubArray ChunkData { get { return _ChunkData; } }
        public int TotalSize { get { return ChunkData.Count; } }
        public int CompressedSize { get { return ChunkData is CompressedByteSubArray ? ((CompressedByteSubArray)ChunkData).CompressedSize : 0; } }
        
        #endregion

        #region Computed properties
        
        public string FourCC
        {
            get
            {
                return Encoding.ASCII.GetString(ChunkData.GetBytes(0, 4));
            }
        }

        public string Name
        {
            get
            {
                return ChunkData.GetString(12, NameLength);
            }
        }

        protected int DataOffset
        {
            get
            {
                return (NameLength + 12 + 7) & -8;
            }
        }

        protected int NameLength
        {
            get
            {
                return ChunkData[6];
            }
        }

        protected int DataLength
        {
            get
            {
                return ChunkData.GetInt32(8);
            }
        }

        protected bool HasData
        {
            get
            {
                return ChunkData[7] == 0;
            }
        }
        
        #endregion

        #region Constructors

        protected RS5Chunk()
        {
            this._Data = new Lazy<ByteSubArray>(() => GetData());
            this._Chunks = new Lazy<Dictionary<string, RS5Chunk>>(() => GetChunks());
        }
        
        protected RS5Chunk(ByteSubArray data, int offset)
            : this()
        {
            int totalsize = (((12 + (int)data[offset + 6] + 7) & -8) + data.GetInt32(offset + 8) + 7) & -8;
            this._ChunkData = new ByteSubArray(data, offset, totalsize);
        }

        #endregion

        #region Lazy-init initializers

        public ByteSubArray GetData()
        {
            if (HasData)
            {
                return new ByteSubArray(ChunkData, DataOffset, DataLength);
            }
            else
            {
                return null;
            }
        }

        private Dictionary<string, RS5Chunk> GetChunks()
        {
            if (!HasData && DataLength != 0)
            {
                Dictionary<string, RS5Chunk> ret = new Dictionary<string, RS5Chunk>();
                int pos = 0;
                while (pos < DataLength)
                {
                    RS5Chunk chunk = new RS5Chunk(ChunkData, DataOffset + pos);
                    ret.Add(chunk.FourCC, chunk);
                    pos += chunk.TotalSize;
                }

                return ret;
            }
            else
            {
                return null;
            }
        }

        #endregion

        public virtual void Flush()
        {
            this._Data = new Lazy<ByteSubArray>(() => GetData());
            this._Chunks = new Lazy<Dictionary<string, RS5Chunk>>(() => GetChunks());
        }
    }

    public class RS5Object : RS5Chunk
    {
        private Lazy<ByteSubArray> _ChunkData;

        public override ByteSubArray ChunkData { get { return _ChunkData.Value; } }

        private Stream FileStream;
        private long Offset;

        public RS5Object(Stream filestream, long offset)
            : base()
        {
            this.FileStream = filestream;
            this.Offset = offset;
            this._ChunkData = new Lazy<ByteSubArray>(() => new CompressedByteSubArray(FileStream, Offset));
        }

        public override void Flush()
        {
            base.Flush();
            this._ChunkData = new Lazy<ByteSubArray>(() => new CompressedByteSubArray(FileStream, Offset));
        }
    }
}

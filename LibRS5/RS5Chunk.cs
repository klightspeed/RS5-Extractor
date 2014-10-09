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

        protected Lazy<SubStream> _Data;
        protected Lazy<Dictionary<string, RS5Chunk>> _Chunks;
        
        #endregion

        #region Lazy-init properties

        public SubStream ChunkData { get; protected set; }
        public SubStream Data { get { return _Data.Value; } }
        public Dictionary<string, RS5Chunk> Chunks { get { return _Chunks.Value; } }
        public long TotalSize { get { return ChunkData.Length; } }
        public long CompressedSize { get { return ChunkData is CompressedSubStream ? ((CompressedSubStream)ChunkData).CompressedSize : 0; } }
        
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
                return ChunkData.GetByte(6);
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
                return ChunkData.GetByte(7) == 0;
            }
        }
        
        #endregion

        #region Constructors

        protected RS5Chunk(SubStream chunkdata)
        {
            this.ChunkData = chunkdata;
            this._Data = new Lazy<SubStream>(() => GetData());
            this._Chunks = new Lazy<Dictionary<string, RS5Chunk>>(() => GetChunks());
        }
        
        protected RS5Chunk(SubStream data, long offset)
            : this(GetChunkData(data, offset))
        {
        }

        #endregion

        #region Lazy-init initializers

        private static SubStream GetChunkData(SubStream data, long offset)
        {
            int totalsize = (((12 + (int)data.GetByte(offset + 6) + 7) & -8) + data.GetInt32(offset + 8) + 7) & -8;
            return new SubStream(data, offset, totalsize);
        }

        private SubStream GetData()
        {
            if (HasData)
            {
                return new SubStream(ChunkData, DataOffset, DataLength);
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
                long pos = 0;
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

        public virtual void Release()
        {
            this._Data = new Lazy<SubStream>(() => GetData());
            this._Chunks = new Lazy<Dictionary<string, RS5Chunk>>(() => GetChunks());
        }
    }

    public class RS5Object : RS5Chunk
    {
        public RS5Object(Stream filestream, long offset, int length, int allocsize)
            : base(new CompressedSubStream(filestream, offset, allocsize))
        {
        }
    }
}

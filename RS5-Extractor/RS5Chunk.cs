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

        protected WeakReference _Data = null;
        public ByteSubArray Data
        {
            get
            {
                ByteSubArray ret = null;

                if (_Data != null)
                {
                    ret = (ByteSubArray)_Data.Target;
                }

                if (ret == null && HasData)
                {
                    ret = new ByteSubArray(ChunkData, DataOffset, DataLength);
                    _Data = new WeakReference(ret);
                }

                return ret;
            }
        }

        protected WeakReference _Chunks = null;
        public Dictionary<string, RS5Chunk> Chunks
        {
            get
            {
                Dictionary<string, RS5Chunk> ret = null;

                if (_Chunks != null)
                {
                    ret = (Dictionary<string, RS5Chunk>)_Chunks.Target;
                }

                if (ret == null && !HasData && DataLength != 0)
                {
                    ret = new Dictionary<string, RS5Chunk>();
                    int pos = 0;
                    while (pos < DataLength)
                    {
                        RS5Chunk chunk = new RS5Chunk(ChunkData, DataOffset + pos);
                        ret.Add(chunk.FourCC, chunk);
                        pos += chunk.TotalSize;
                    }
                    _Chunks = new WeakReference(ret);
                }

                return ret;
            }
        }

        protected ByteSubArray ParentData = null;
        protected Stream FileStream;
        protected long FileOffset;
        public int CompressedSize;
        protected int TotalSize;

        protected WeakReference _ChunkData = null;
        public ByteSubArray ChunkData
        {
            get
            {
                ByteSubArray ret = null;

                if (ParentData != null)
                {
                    return ParentData;
                }

                if (_ChunkData != null)
                {
                    ret = (ByteSubArray)_ChunkData.Target;
                }

                if (ret == null)
                {
                    FileStream.Seek(FileOffset, SeekOrigin.Begin);
                    using (ZlibStream zstream = new ZlibStream(FileStream, CompressionMode.Decompress, true))
                    {
                        using (MemoryStream outstrm = new MemoryStream())
                        {
                            zstream.CopyTo(outstrm);
                            ret = new ByteSubArray(outstrm.ToArray());
                            this.CompressedSize = (int)zstream.TotalIn;
                            this.TotalSize = (int)zstream.TotalOut;
                        }
                    }
                    _ChunkData = new WeakReference(ret);
                }

                return ret;
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

        public RS5Chunk(Stream filestream, long offset)
        {
            this.FileStream = filestream;
            this.FileOffset = offset;
        }

        protected RS5Chunk(ByteSubArray data, int offset)
        {
            this.TotalSize = (((12 + (int)data[offset + 6] + 7) & -8) + data.GetInt32(offset + 8) + 7) & -8;
            this.ParentData = new ByteSubArray(data, offset, this.TotalSize);
        }
    }
}

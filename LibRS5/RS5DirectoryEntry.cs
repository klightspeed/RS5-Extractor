using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ionic.Zlib;


namespace LibRS5
{
    public class RS5DirectoryEntry
    {
        private WeakReference DataRef;
        private Func<RS5Chunk> DataFactory;
        private RS5Chunk PersistentData;

        public DateTime ModTime { get; private set; }
        public string Name { get; private set; }
        public string Type { get; private set; }

        public RS5Chunk Data
        {
            get
            {
                RS5Chunk data = PersistentData;

                if (data == null)
                {
                    data = DataRef == null ? null : DataRef.Target as RS5Chunk;

                    if (data == null)
                    {
                        data = DataFactory();
                        DataRef = new WeakReference(data);
                    }
                }
                return data;
            }
        }

        public RS5DirectoryEntry(Stream filestream, byte[] direntData)
        {
            long dataoffset = BitConverter.ToInt64(direntData, 0);
            int datalength = BitConverter.ToInt32(direntData, 8);
            this.Type = Encoding.ASCII.GetString(direntData, 20, 4);
            int allocsize = BitConverter.ToInt32(direntData, 24);
            this.ModTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(direntData, 32));
            this.Name = Encoding.ASCII.GetString(direntData.Skip(40).TakeWhile(c => c != 0).ToArray());
            this.DataFactory = () => new RS5Object(filestream, dataoffset, datalength, allocsize);
        }

        public RS5DirectoryEntry(RS5Chunk chunk, string name, string type, DateTime modtime)
        {
            this.PersistentData = chunk;
            this.Name = name;
            this.Type = type;
            this.ModTime = modtime;
        }

        public long Write(Stream filestream, long direntoffset, long dataoffset)
        {
            RS5Chunk data = Data;
            filestream.Seek(dataoffset, SeekOrigin.Begin);
            
            using (ZlibStream zstream = new ZlibStream(filestream, CompressionMode.Compress, true))
            {
                data.ChunkData.CopyTo(zstream);
            }

            long comprlen = filestream.Position - dataoffset;
            byte[] direntdata = new byte[168];
            
            Array.Copy(BitConverter.GetBytes(dataoffset), 0, direntdata, 0, 8);
            Array.Copy(BitConverter.GetBytes(comprlen), 0, direntdata, 8, 4);
            Array.Copy(BitConverter.GetBytes(0x80000000UL), 0, direntdata, 12, 4);
            Array.Copy(BitConverter.GetBytes(0x00000300UL), 0, direntdata, 16, 4);
            Encoding.ASCII.GetBytes(Type, 0, Type.Length > 4 ? 4 : Type.Length, direntdata, 20);
            Array.Copy(BitConverter.GetBytes((long)data.TotalSize * 2), 0, direntdata, 24, 8);
            Array.Copy(BitConverter.GetBytes(ModTime.ToFileTimeUtc()), 0, direntdata, 32, 8);
            Encoding.ASCII.GetBytes(Name, 0, Name.Length > 127 ? 127 : Name.Length, direntdata, 40);
            
            filestream.Seek(direntoffset, SeekOrigin.Begin);
            filestream.Write(direntdata, 0, direntdata.Length);
            return comprlen;
        }
    }
}

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

        public bool IsCompressed { get; private set; }
        public long DataOffset { get; private set; }
        public int DataLength { get; private set; }
        public int AllocSize { get; private set; }

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

        public RS5DirectoryEntry(Stream filestream, long dataoffset, int datalength, int allocsize, bool iscompressed, string name, string type, DateTime modtime)
        {
            this.Name = name;
            this.Type = type;
            this.ModTime = modtime;
            this.IsCompressed = iscompressed;
            this.DataOffset = dataoffset;
            this.DataLength = datalength;
            this.AllocSize = allocsize;
            this.DataFactory = () => new RS5Object(filestream, dataoffset, datalength, allocsize, iscompressed);
        }

        public RS5DirectoryEntry(RS5Chunk chunk, string name, string type, DateTime modtime)
        {
            this.PersistentData = chunk;
            this.Name = name;
            this.Type = type;
            this.ModTime = modtime;
        }
    }
}

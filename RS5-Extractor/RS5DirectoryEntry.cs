using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RS5_Extractor
{
    public class RS5DirectoryEntry
    {
        private WeakReference DataRef;
        private Func<RS5Chunk> DataFactory;

        public DateTime ModTime { get; private set; }
        public string Name { get; private set; }
        public string Type { get; private set; }

        public RS5Chunk Data
        {
            get
            {
                RS5Chunk data = DataRef == null ? null : DataRef.Target as RS5Chunk;

                if (data == null)
                {
                    data = DataFactory();
                    DataRef = new WeakReference(data);
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
    }
}

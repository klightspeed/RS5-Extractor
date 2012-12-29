using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RS5_Extractor
{
    public class RS5DirectoryEntry
    {
        public DateTime ModTime { get; private set; }
        public string Name { get; private set; }
        public string Type { get; private set; }

        private Stream FileStream;
        private int DataLength;
        private long DataOffset;

        public RS5Chunk GetData()
        {
            return new RS5Object(FileStream, DataOffset);
        }

        public RS5DirectoryEntry(Stream filestream, byte[] direntData)
        {
            this.FileStream = filestream;
            this.DataOffset = BitConverter.ToInt64(direntData, 0);
            this.DataLength = BitConverter.ToInt32(direntData, 8);
            this.Type = Encoding.ASCII.GetString(direntData, 20, 4);
            this.ModTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(direntData, 32));
            this.Name = Encoding.ASCII.GetString(direntData.Skip(40).TakeWhile(c => c != 0).ToArray());
        }
    }
}

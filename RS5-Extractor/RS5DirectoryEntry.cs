using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RS5_Extractor
{
    public class RS5DirectoryEntry
    {
        public DateTime ModTime;
        public string Name;
        public string Type;
        public RS5Chunk Data;

        public RS5DirectoryEntry(Stream filestream, byte[] direntData)
        {

            long dataoffset = BitConverter.ToInt64(direntData, 0);
            int datalength = BitConverter.ToInt32(direntData, 8);
            this.Type = Encoding.ASCII.GetString(direntData, 20, 4);
            this.ModTime = DateTime.FromFileTimeUtc(BitConverter.ToInt64(direntData, 32));
            this.Name = Encoding.ASCII.GetString(direntData.Skip(40).TakeWhile(c => c != 0).ToArray());
            this.Data = new RS5Chunk(filestream, dataoffset);
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RS5_Extractor
{
    public class RS5Directory : SortedDictionary<string, RS5DirectoryEntry>
    {
        public RS5Directory(Stream filestrm, long directory_offset, int dirent_length)
            : base()
        {
            byte[] dirent_data = new byte[dirent_length];
            filestrm.Seek(directory_offset, SeekOrigin.Begin);
            filestrm.Read(dirent_data, 0, dirent_length);
            long offset = BitConverter.ToInt64(dirent_data, 0);
            long length = BitConverter.ToInt32(dirent_data, 8);
            int flags = BitConverter.ToInt32(dirent_data, 12);
            if (offset == directory_offset)
            {
                for (int i = 1; i < (length / dirent_length); i++)
                {
                    filestrm.Seek(directory_offset + i * dirent_length, SeekOrigin.Begin);
                    filestrm.Read(dirent_data, 0, dirent_length);
                    if (dirent_data.Take(12).Select(c => c != 0).Aggregate((a, b) => (a || b)))
                    {
                        RS5DirectoryEntry dirent = new RS5DirectoryEntry(filestrm, dirent_data);
                        this[dirent.Name] = dirent;
                    }
                }
            }
        }
    }
}

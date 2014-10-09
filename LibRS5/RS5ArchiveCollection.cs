using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibRS5
{
    public class RS5ArchiveCollection : List<RS5Archive>
    {
        public Dictionary<string, RS5DirectoryEntry> GetDirectory()
        {
            Dictionary<string, RS5DirectoryEntry> directory = new Dictionary<string, RS5DirectoryEntry>();

            foreach (RS5Archive archive in this)
            {
                foreach (RS5DirectoryEntry dirent in archive)
                {
                    if (!directory.ContainsKey(dirent.Name))
                    {
                        directory[dirent.Name] = dirent;
                    }
                }
            }

            return directory;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LibRS5
{
    public class RS5Archive
    {
        private Stream ArchiveStream { get; set; }
        public Dictionary<string, RS5DirectoryEntry> CentralDirectory { get; set; }

        public static RS5Archive Open(Stream filestrm)
        {
            RS5Archive archive = new RS5Archive();
            archive.OpenExisting(filestrm);
            return archive;
        }

        protected RS5Archive()
        {
        }

        protected void OpenExisting(Stream filestrm)
        {
            this.ArchiveStream = filestrm;
            this.CentralDirectory = new Dictionary<string, RS5DirectoryEntry>();
            filestrm.Seek(0, SeekOrigin.Begin);
            byte[] fileheader = new byte[24];
            filestrm.Read(fileheader, 0, 24);
            string magic = Encoding.ASCII.GetString(fileheader, 0, 8);
            if (magic == "CFILEHDR")
            {
                long directory_offset = BitConverter.ToInt64(fileheader, 8);
                int dirent_length = BitConverter.ToInt32(fileheader, 16);
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
                            CentralDirectory[dirent.Name] = dirent;
                        }
                    }
                }
                else
                {
                    throw new InvalidDataException("Central Directory file 0 is not Central Directory");
                }
            }
            else
            {
                throw new InvalidDataException("File is not an RS5 file");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ionic.Zlib;

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

        protected RS5DirectoryEntry GetDirectoryEntry(byte[] direntData)
        {
            long dataoffset = BitConverter.ToInt64(direntData, 0);
            int datalength = BitConverter.ToInt32(direntData, 8);
            string type = Encoding.ASCII.GetString(direntData, 20, 4);
            int allocsize = BitConverter.ToInt32(direntData, 24);
            bool iscompressed = (allocsize & 1) != 0;
            allocsize = (allocsize == 0) ? datalength : allocsize;
            long timestamp = BitConverter.ToInt64(direntData, 32);
            DateTime modtime = timestamp == 0 ? DateTime.MinValue : DateTime.FromFileTimeUtc(timestamp);
            string name = Encoding.ASCII.GetString(direntData.Skip(40).TakeWhile(c => c != 0).ToArray());

            return new RS5DirectoryEntry(ArchiveStream, dataoffset, datalength, allocsize, iscompressed, name, type, modtime);
        }

        protected byte[] GetDirentBytes(long dataoffset, int datalength, int allocsize, bool iscompressed, string name, string type, DateTime modtime)
        {
            byte[] direntdata = new byte[168];

            Array.Copy(BitConverter.GetBytes(dataoffset), 0, direntdata, 0, 8);
            Array.Copy(BitConverter.GetBytes(datalength), 0, direntdata, 8, 4);
            
            if (iscompressed)
            {
                Array.Copy(BitConverter.GetBytes(0x80000000UL), 0, direntdata, 12, 4);
                Array.Copy(BitConverter.GetBytes(0x00000300UL), 0, direntdata, 16, 4);
                Array.Copy(BitConverter.GetBytes(((long)allocsize << 1) | 1), 0, direntdata, 24, 8);
            }

            Encoding.ASCII.GetBytes(type, 0, type.Length > 4 ? 4 : type.Length, direntdata, 20);
            Array.Copy(BitConverter.GetBytes(modtime == DateTime.MinValue ? 0 : modtime.ToFileTimeUtc()), 0, direntdata, 32, 8);
            Encoding.ASCII.GetBytes(name, 0, name.Length > 127 ? 127 : name.Length, direntdata, 40);

            return direntdata;
        }

        protected long Write(RS5Chunk chunk, long direntoffset, long dataoffset, string name, string type, DateTime modtime)
        {
            ArchiveStream.Seek(dataoffset, SeekOrigin.Begin);

            using (ZlibStream zstream = new ZlibStream(ArchiveStream, CompressionMode.Compress, true))
            {
                chunk.ChunkData.CopyTo(zstream);
            }

            long comprlen = ArchiveStream.Position - dataoffset;
            byte[] direntdata = GetDirentBytes(dataoffset, (int)comprlen, (int)chunk.TotalSize, true, name, type, modtime);

            ArchiveStream.Seek(direntoffset, SeekOrigin.Begin);
            ArchiveStream.Write(direntdata, 0, direntdata.Length);
            return comprlen;
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
                            RS5DirectoryEntry dirent = GetDirectoryEntry(dirent_data);
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

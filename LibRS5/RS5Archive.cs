using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ionic.Zlib;

namespace LibRS5
{
    public class RS5Archive : IEnumerable<RS5DirectoryEntry>
    {
        private Stream ArchiveStream { get; set; }
        private RS5DirectoryEntry[] CentralDirectoryEntries { get; set; }
        private Dictionary<string, int> CentralDirectoryLookup { get; set; }

        private int DirectoryEntryLength { get; set; }

        public static RS5Archive Open(Stream filestrm)
        {
            RS5Archive archive = new RS5Archive();
            archive.OpenExisting(filestrm);
            return archive;
        }

        protected RS5Archive()
        {
        }

        protected RS5DirectoryEntry GetDirectoryEntry(byte[] direntData, int offset)
        {
            long dataoffset = BitConverter.ToInt64(direntData, offset + 0);
            int datalength = BitConverter.ToInt32(direntData, offset + 8);
            string type = Encoding.ASCII.GetString(direntData, offset + 20, 4);
            int allocsize = BitConverter.ToInt32(direntData, offset + 24);
            bool iscompressed = (allocsize & 1) != 0;
            allocsize = (allocsize == 0) ? datalength : allocsize;
            long timestamp = BitConverter.ToInt64(direntData, offset + 32);
            DateTime modtime = timestamp == 0 ? DateTime.MinValue : DateTime.FromFileTimeUtc(timestamp);
            byte[] namebytes = new byte[DirectoryEntryLength - 40];
            Array.Copy(direntData, 40, namebytes, 0, DirectoryEntryLength - 40);
            string name = Encoding.ASCII.GetString(namebytes.TakeWhile(c => c != 0).ToArray());

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
            this.ArchiveStream.Seek(0, SeekOrigin.Begin);
            byte[] fileheader = new byte[24];
            this.ArchiveStream.Read(fileheader, 0, 24);
            string magic = Encoding.ASCII.GetString(fileheader, 0, 8);
            if (magic == "CFILEHDR")
            {
                long offset = BitConverter.ToInt64(fileheader, 8);
                this.DirectoryEntryLength = BitConverter.ToInt32(fileheader, 16);
                byte[] direntdata = new byte[this.DirectoryEntryLength];
                this.ArchiveStream.Seek(offset, SeekOrigin.Begin);
                this.ArchiveStream.Read(direntdata, 0, DirectoryEntryLength);
                RS5DirectoryEntry centralDirectoryNode = GetDirectoryEntry(direntdata, 0);

                if (offset == centralDirectoryNode.DataOffset)
                {
                    byte[] directoryData = centralDirectoryNode.Data.ChunkData.ReadBytes(centralDirectoryNode.DataLength);
                    int nrents = directoryData.Length / DirectoryEntryLength;
                    CentralDirectoryEntries = new RS5DirectoryEntry[nrents];
                    CentralDirectoryLookup = new Dictionary<string, int>();
                    CentralDirectoryEntries[0] = centralDirectoryNode;

                    for (int i = 1; i < nrents; i++)
                    {
                        RS5DirectoryEntry dirent = GetDirectoryEntry(directoryData, i * DirectoryEntryLength);
                        CentralDirectoryEntries[i] = dirent;
                        CentralDirectoryLookup[dirent.Name] = i;
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

        public IEnumerator<RS5DirectoryEntry> GetEnumerator()
        {
            foreach (RS5DirectoryEntry dirent in CentralDirectoryEntries)
            {
                if (dirent != null && dirent.Name != "")
                {
                    yield return dirent;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool ContainsKey(string name)
        {
            return CentralDirectoryLookup.ContainsKey(name);
        }

        public RS5DirectoryEntry this[string name]
        {
            get
            {
                return CentralDirectoryEntries[CentralDirectoryLookup[name]];
            }
            set
            {

            }
        }
    }
}

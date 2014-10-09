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
        const string FileMagic = "CFILEHDR";

        private Stream ArchiveStream { get; set; }
        private RS5DirectoryEntry[] CentralDirectoryEntries { get; set; }
        private Dictionary<string, int> CentralDirectoryLookup { get; set; }

        private int DirectoryEntryLength { get; set; }

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
            DateTime modtime = timestamp <= 0 ? DateTime.MinValue : DateTime.FromFileTimeUtc(timestamp);
            byte[] namebytes = new byte[DirectoryEntryLength - 40];
            Array.Copy(direntData, offset + 40, namebytes, 0, DirectoryEntryLength - 40);
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

        protected byte[] GetDirentBytes(RS5DirectoryEntry dirent)
        {
            return GetDirentBytes(dirent.DataOffset, dirent.DataLength, dirent.AllocSize, dirent.IsCompressed, dirent.Name, dirent.Type, dirent.ModTime);
        }

        protected int AddDirEntry(long dataOffset, int dataLength, int allocSize, bool isCompressed, string name, string type, DateTime modtime)
        {
            RS5DirectoryEntry dirent = new RS5DirectoryEntry(this.ArchiveStream, dataOffset, dataLength, allocSize, isCompressed, name, type, modtime);

            return AddDirEntry(dirent);
        }

        protected int AddDirEntry(RS5DirectoryEntry dirent)
        {
            int direntIndex;

            if (this.CentralDirectoryLookup.ContainsKey(dirent.Name))
            {
                direntIndex = this.CentralDirectoryLookup[dirent.Name];
            }
            else
            {
                direntIndex = this.CentralDirectoryEntries.Length;

                for (int i = 1; i < this.CentralDirectoryEntries.Length; i++)
                {
                    if (this.CentralDirectoryEntries[i] == null)
                    {
                        direntIndex = i;
                        break;
                    }
                }

                if (direntIndex >= this.CentralDirectoryEntries.Length)
                {
                    ReallocDirectory(4096);
                    return AddDirEntry(dirent);
                }
            }

            this.CentralDirectoryEntries[direntIndex] = dirent;
            this.CentralDirectoryLookup[dirent.Name] = direntIndex;

            return direntIndex;
        }

        protected void OpenExisting(Stream filestrm)
        {
            this.ArchiveStream = filestrm;
            this.ArchiveStream.Seek(0, SeekOrigin.Begin);
            byte[] fileheader = new byte[24];
            this.ArchiveStream.Read(fileheader, 0, 24);
            string magic = Encoding.ASCII.GetString(fileheader, 0, 8);
            if (magic == FileMagic)
            {
                long offset = BitConverter.ToInt64(fileheader, 8);
                this.DirectoryEntryLength = BitConverter.ToInt32(fileheader, 16);
                byte[] direntdata = new byte[this.DirectoryEntryLength];
                this.ArchiveStream.Seek(offset, SeekOrigin.Begin);
                this.ArchiveStream.Read(direntdata, 0, DirectoryEntryLength);
                RS5DirectoryEntry centralDirectoryNode = GetDirectoryEntry(direntdata, 0);

                if (offset == centralDirectoryNode.DataOffset)
                {
                    byte[] directoryData = centralDirectoryNode.Data.ChunkData.GetBytes(0, centralDirectoryNode.DataLength);
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

        protected void CreateNew(Stream filestrm)
        {
            this.ArchiveStream = filestrm;
            this.CentralDirectoryLookup = new Dictionary<string, int>();
            this.CentralDirectoryEntries = new RS5DirectoryEntry[4096];
            this.DirectoryEntryLength = 168;
            byte[] fileheader = new byte[24];
            Array.Copy(Encoding.ASCII.GetBytes(FileMagic), fileheader, 8);
            Array.Copy(BitConverter.GetBytes(24), 0, fileheader, 8, 8);
            Array.Copy(BitConverter.GetBytes(this.DirectoryEntryLength), 0, fileheader, 16, 4);
            this.ArchiveStream.Seek(0, SeekOrigin.Begin);
            this.ArchiveStream.Write(fileheader, 0, 24);

            RS5DirectoryEntry root = new RS5DirectoryEntry(this.ArchiveStream, 24, 4096 * this.DirectoryEntryLength, 0, false, "", "", DateTime.MinValue);
            byte[] directorydata = new byte[4096 * this.DirectoryEntryLength];
            Array.Copy(GetDirentBytes(root), 0, directorydata, 0, this.DirectoryEntryLength);
            this.ArchiveStream.Seek(24, SeekOrigin.Begin);
            this.ArchiveStream.Write(directorydata, 0, directorydata.Length);
            this.CentralDirectoryEntries[0] = root;
        }

        public void Write(RS5Chunk data, string name, string type, DateTime modtime)
        {
            Write(new RS5DirectoryEntry(data, name, type, modtime));
        }

        public void Write(RS5DirectoryEntry dirent)
        {
            long dataOffset = this.ArchiveStream.Length;

            this.ArchiveStream.Seek(dataOffset, SeekOrigin.Begin);

            using (ZlibStream zstream = new ZlibStream(this.ArchiveStream, CompressionMode.Compress, true))
            {
                dirent.Data.ChunkData.CopyTo(zstream);
            }

            long comprlen = this.ArchiveStream.Position - dataOffset;

            int direntIndex = AddDirEntry(dataOffset, (int)comprlen, (int)dirent.Data.TotalSize, true, dirent.Name, dirent.Type, dirent.ModTime);

            byte[] direntdata = GetDirentBytes(this.CentralDirectoryEntries[direntIndex]);

            this.ArchiveStream.Seek(this.CentralDirectoryEntries[0].DataOffset + direntIndex * this.DirectoryEntryLength, SeekOrigin.Begin);
            this.ArchiveStream.Write(direntdata, 0, direntdata.Length);
        }

        public void ReallocDirectory(int minfree)
        {
            long diroffset = this.ArchiveStream.Length;

            RS5DirectoryEntry[] dirents = this.CentralDirectoryEntries;

            Array.Resize(ref dirents, dirents.Length + minfree);

            byte[] dirdata = new byte[dirents.Length * this.DirectoryEntryLength];

            RS5DirectoryEntry oldroot = this.CentralDirectoryEntries[0];

            dirents[0] = new RS5DirectoryEntry(this.ArchiveStream, diroffset, dirents.Length * this.DirectoryEntryLength, 0, false, "", "", DateTime.MinValue);

            for (int i = 0; i < dirents.Length; i++)
            {
                if (dirents[i] != null)
                {
                    byte[] direntdata = GetDirentBytes(dirents[i]);
                    Array.Copy(direntdata, 0, dirdata, i * this.DirectoryEntryLength, direntdata.Length);
                }
            }

            this.ArchiveStream.Seek(diroffset, SeekOrigin.Begin);
            this.ArchiveStream.Write(dirdata, 0, dirdata.Length);

            this.CentralDirectoryEntries = dirents;

            AddDirEntry(oldroot.DataOffset, oldroot.DataLength, oldroot.AllocSize, oldroot.IsCompressed, oldroot.Name, oldroot.Type, oldroot.ModTime);

            this.ArchiveStream.Seek(8, SeekOrigin.Begin);
            this.ArchiveStream.Write(BitConverter.GetBytes(diroffset), 0, 8);
        }

        public static RS5Archive Open(Stream filestrm)
        {
            RS5Archive archive = new RS5Archive();
            archive.OpenExisting(filestrm);
            return archive;
        }

        public static RS5Archive Create(Stream filestrm)
        {
            RS5Archive archive = new RS5Archive();
            archive.CreateNew(filestrm);
            return archive;
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

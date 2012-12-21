using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace RS5_Extractor
{
    class Program
    {
        private static RS5Directory ProcessRS5File(Stream filestrm)
        {
            filestrm.Seek(0, SeekOrigin.Begin);
            byte[] fileheader = new byte[24];
            filestrm.Read(fileheader, 0, 24);
            string magic = Encoding.ASCII.GetString(fileheader, 0, 8);
            if (magic == "CFILEHDR")
            {
                long directory_offset = BitConverter.ToInt64(fileheader, 8);
                int dirent_length = BitConverter.ToInt32(fileheader, 16);
                return new RS5Directory(filestrm, directory_offset, dirent_length);
            }
            else
            {
                throw new InvalidDataException("File is not an RS5 file");
            }
        }

        private static void Main(string[] args)
        {
            Stream filestrm = File.Open(args[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Console.Write("Processing central directory ... ");
            RS5Directory dirents = ProcessRS5File(filestrm);
            Console.WriteLine("Done");

            Console.WriteLine("Processing Textures ... ");
            foreach (KeyValuePair<string, RS5DirectoryEntry> dirent in dirents.Where(d => d.Value.Type == "IMAG"))
            {
                Texture.AddTexture(dirent.Value);
                Texture texture = Texture.GetTexture(dirent.Key);
                if (!File.Exists(texture.PNGFilename) && !File.Exists(texture.DDSFilename))
                {
                    Console.WriteLine("Saving texture {0}", dirent.Key);
                    texture.Save();
                }
            }

            Console.WriteLine("Processing Immobile Models ... ");
            foreach (KeyValuePair<string, RS5DirectoryEntry> dirent in dirents.Where(d => d.Value.Type == "IMDL"))
            {
                ImmobileModel model = new ImmobileModel(dirent.Value);
                if (!File.Exists(model.ColladaMultimeshFilename))
                {
                    Console.WriteLine("Saving immobile model {0}", dirent.Key);
                    model.SaveMultimesh();
                    model.Save();
                }
            }

            Console.WriteLine("Processing Animated Models ... ");
            foreach (KeyValuePair<string, RS5DirectoryEntry> dirent in dirents.Where(d => d.Value.Type == "AMDL"))
            {
                AnimatedModel model = new AnimatedModel(dirent.Value);
                if (!File.Exists(model.ColladaMultimeshFilename))
                {
                    Console.WriteLine("Saving animated model {0}", dirent.Key);

                    model.Save();

                    if (model.Textures.Count != 1)
                    {
                        model.SaveMultimesh();
                    }
                    
                    if (model.IsAnimated)
                    {
                        model.SaveAnimated();
                    }
                }
            }
        }
    }
}

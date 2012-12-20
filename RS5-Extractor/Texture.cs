using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;

namespace RS5_Extractor
{
    public class Texture
    {
        protected static Dictionary<string, Texture> Textures = new Dictionary<string, Texture>();
        protected RS5Chunk Chunk;

        public string Name { get; protected set; }
        public DateTime ModTime { get; protected set; }

        public string PNGFilename
        {
            get
            {
                return ".\\" + Name + ".png";
            }
        }

        public string DDSFilename
        {
            get
            {
                return ".\\" + Name + ".dds";
            }
        }

        protected WeakReference _Data = null;
        public byte[] Data
        {
            get
            {
                byte[] ret = null;
                if (_Data != null)
                {
                    ret = (byte[])_Data.Target;
                }
                if (ret == null)
                {
                    ret = Chunk.Chunks["DATA"].Data.ToArray();
                    _Data = new WeakReference(ret);
                }
                return ret;
            }
        }

        protected Color[,] GetDXT1ColourBlock(byte[] data, int offset, bool IsDXT3)
        {
            ushort c0v = BitConverter.ToUInt16(data, offset);
            ushort c1v = BitConverter.ToUInt16(data, offset + 2);
            Color[] c = new Color[4];

            c[0] = Color.FromArgb(0xFF, (c0v >> 8) & 0xF8, (c0v >> 3) & 0xFC, (c0v << 3) & 0xF8);
            c[1] = Color.FromArgb(0xFF, (c1v >> 8) & 0xF8, (c1v >> 3) & 0xFC, (c1v << 3) & 0xF8);

            if (c0v > c1v || IsDXT3)
            {
                c[2] = Color.FromArgb(0xFF, ((int)c[0].R * 2 + c[1].R + 1) / 3, ((int)c[0].G * 2 + c[1].G + 1) / 3, ((int)c[0].B * 2 + c[1].B + 1) / 3);
                c[3] = Color.FromArgb(0xFF, ((int)c[1].R * 2 + c[0].R + 1) / 3, ((int)c[1].G * 2 + c[0].G + 1) / 3, ((int)c[1].B * 2 + c[0].B + 1) / 3);
            }
            else
            {
                c[2] = Color.FromArgb(0xFF, ((int)c[0].R + c[1].R) / 2, ((int)c[0].G + c[1].G) / 2, ((int)c[0].B + c[1].B) / 2);
                c[3] = Color.FromArgb(0, 0, 0, 0);
            }

            ulong lookup = BitConverter.ToUInt32(data, offset + 4);
            Color[,] output = new Color[4, 4];

            for (int i = 0; i < 16; i++)
            {
                output[i % 4, i / 4] = c[(lookup >> (i * 2)) & 3];
            }

            return output;
        }

        protected Bitmap GetBitmapFromDDS_DXT1(byte[] data, int offset, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            byte[] bmpraw = new byte[width * height * 4];

            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    Color[,] colourdata = GetDXT1ColourBlock(data, offset, false);
                    for (int v = 0; v < 4; v++)
                    {
                        for (int u = 0; u < 4; u++)
                        {
                            if (x + u < width && y + v < height)
                            {
                                int i = ((y + v) * width + (x + u)) * 4;
                                bmpraw[i] = colourdata[u, v].B;
                                bmpraw[i + 1] = colourdata[u, v].G;
                                bmpraw[i + 2] = colourdata[u, v].R;
                                bmpraw[i + 3] = colourdata[u, v].B;
                            }
                        }
                    }
                    offset += 8;
                }
            }

            BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(bmpraw, y * width * 4, bmpdata.Scan0 + y * bmpdata.Stride, width * 4);
            }

            bmp.UnlockBits(bmpdata);

            return bmp;
        }

        protected Bitmap GetBitmapFromDDS_DXT3(byte[] data, int offset, int width, int height, bool IsDXT2)
        {
            Bitmap bmp = new Bitmap(width, height, IsDXT2 ? PixelFormat.Format32bppPArgb : PixelFormat.Format32bppArgb);
            byte[] bmpraw = new byte[width * height * 4];

            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    ulong alpharaw = BitConverter.ToUInt64(data, offset);
                    byte[,] alphadata = new byte[4, 4];

                    for (int i = 0; i < 16; i++)
                    {
                        alphadata[i % 4, i / 4] = (byte)(((alpharaw >> (i * 4)) & 0x0F) * 0x11);
                    }

                    Color[,] colourdata = GetDXT1ColourBlock(data, offset + 8, true);

                    for (int v = 0; v < 4; v++)
                    {
                        for (int u = 0; u < 4; u++)
                        {
                            if (x + u < width && y + v < height)
                            {
                                int i = ((y + v) * width + (x + u)) * 4;
                                bmpraw[i] = colourdata[u, v].B;
                                bmpraw[i + 1] = colourdata[u, v].G;
                                bmpraw[i + 2] = colourdata[u, v].R;
                                bmpraw[i + 3] = alphadata[u, v];
                            }
                        }
                    }

                    offset += 16;
                }
            }

            BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, IsDXT2 ? PixelFormat.Format32bppPArgb : PixelFormat.Format32bppArgb);

            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(bmpraw, y * width * 4, bmpdata.Scan0 + y * bmpdata.Stride, width * 4);
            }

            bmp.UnlockBits(bmpdata);

            return bmp;
        }

        protected Bitmap GetBitmapFromDDS_DXT5(byte[] data, int offset, int width, int height, bool IsDXT4)
        {
            Bitmap bmp = new Bitmap(width, height, IsDXT4 ? PixelFormat.Format32bppPArgb : PixelFormat.Format32bppArgb);
            byte[] bmpraw = new byte[width * height * 4];

            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    byte[] a = new byte[8];
                    a[0] = data[offset];
                    a[1] = data[offset + 1];

                    if (a[0] > a[1])
                    {
                        a[2] = (byte)(((int)a[0] * 6 + a[1] * 1 + 3) / 7);
                        a[3] = (byte)(((int)a[0] * 5 + a[1] * 2 + 3) / 7);
                        a[4] = (byte)(((int)a[0] * 4 + a[1] * 3 + 3) / 7);
                        a[5] = (byte)(((int)a[0] * 3 + a[1] * 4 + 3) / 7);
                        a[6] = (byte)(((int)a[0] * 2 + a[1] * 5 + 3) / 7);
                        a[7] = (byte)(((int)a[0] * 1 + a[1] * 6 + 3) / 7);
                    }
                    else
                    {
                        a[2] = (byte)(((int)a[0] * 4 + a[1] * 1 + 2) / 5);
                        a[3] = (byte)(((int)a[0] * 3 + a[1] * 2 + 2) / 5);
                        a[4] = (byte)(((int)a[0] * 2 + a[1] * 3 + 2) / 5);
                        a[5] = (byte)(((int)a[0] * 1 + a[1] * 4 + 2) / 5);
                        a[6] = 0x00;
                        a[7] = 0xFF;
                    }

                    ulong alphasel = BitConverter.ToUInt64(data, offset) >> 16;
                    byte[,] alphadata = new byte[4, 4];

                    for (int i = 0; i < 16; i++)
                    {
                        alphadata[i % 4, i / 4] = a[(alphasel >> (i * 3)) & 7];
                    }

                    Color[,] colourdata = GetDXT1ColourBlock(data, offset + 8, true);

                    for (int v = 0; v < 4; v++)
                    {
                        for (int u = 0; u < 4; u++)
                        {
                            if (x + u < width && y + v < height)
                            {
                                int i = ((y + v) * width + (x + u)) * 4;
                                bmpraw[i] = colourdata[u, v].B;
                                bmpraw[i + 1] = colourdata[u, v].G;
                                bmpraw[i + 2] = colourdata[u, v].R;
                                bmpraw[i + 3] = alphadata[u, v];
                            }
                        }
                    }

                    offset += 16;
                }
            }

            BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, IsDXT4 ? PixelFormat.Format32bppPArgb : PixelFormat.Format32bppArgb);

            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(bmpraw, y * width * 4, bmpdata.Scan0 + y * bmpdata.Stride, width * 4);
            }

            bmp.UnlockBits(bmpdata);

            return bmp;
        }

        protected int MostSignificantBitPosition(uint v)
        {
            for (int i = 31; i >= 0; i--)
            {
                if ((v >> i) != 0)
                {
                    return i;
                }
            }
            return -1;
        }

        protected byte GetMaskShiftVal(uint v, uint mask, int shift, byte valifmaskzero)
        {
            if (mask == 0)
            {
                return valifmaskzero;
            }
            else
            {
                v &= mask;
                if (shift > 0)
                {
                    return (byte)(v >> shift);
                }
                else if (shift < 0)
                {
                    return (byte)(v << -shift);
                }
                else
                {
                    return (byte)v;
                }
            }
        }

        protected Bitmap GetBitmapFromDDS_RAW(byte[] data)
        {
            uint flags = BitConverter.ToUInt32(data, 8);
            int height = BitConverter.ToInt32(data, 12);
            int width = BitConverter.ToInt32(data, 16);
            int pitch = BitConverter.ToInt32(data, 20);
            int rgbbitcount = BitConverter.ToInt32(data, 88);
            uint redmask = BitConverter.ToUInt32(data, 92);
            uint grnmask = BitConverter.ToUInt32(data, 96);
            uint blumask = BitConverter.ToUInt32(data, 100);
            uint alphamask = BitConverter.ToUInt32(data, 104);

            int redshift = MostSignificantBitPosition(redmask) - 7;
            int grnshift = MostSignificantBitPosition(grnmask) - 7;
            int blushift = MostSignificantBitPosition(blumask) - 7;
            int alphashift = MostSignificantBitPosition(alphamask) - 7;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            byte[] bmpraw = new byte[width * height * 4];

            if ((flags & 8) == 0)
            {
                pitch = width * rgbbitcount / 8;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * pitch + x * (rgbbitcount / 8);
                    int o = (y * width + x) * 4;
                    uint v = BitConverter.ToUInt32(data, i + 128 - (rgbbitcount / 8)) >> (32 - rgbbitcount);
                    bmpraw[o + 0] = GetMaskShiftVal(v, blumask, blushift, 0x00);
                    bmpraw[o + 1] = GetMaskShiftVal(v, grnmask, grnshift, 0x00);
                    bmpraw[o + 2] = GetMaskShiftVal(v, redmask, redshift, 0x00);
                    bmpraw[o + 3] = GetMaskShiftVal(v, alphamask, alphashift, 0xFF);
                }
            }
            BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(bmpraw, y * width * 4, bmpdata.Scan0 + y * bmpdata.Stride, width * 4);
            }

            bmp.UnlockBits(bmpdata);

            return bmp;
        }

        protected Bitmap GetBitmapFromDDS(byte[] data)
        {
            int height = BitConverter.ToInt32(data, 12);
            int width = BitConverter.ToInt32(data, 16);
            string fourcc = Encoding.ASCII.GetString(data, 84, 4);

            switch (fourcc)
            {
                case "DXT1":
                    return GetBitmapFromDDS_DXT1(data, 128, width, height);
                case "DXT2":
                    return GetBitmapFromDDS_DXT3(data, 128, width, height, true);
                case "DXT3":
                    return GetBitmapFromDDS_DXT3(data, 128, width, height, false);
                case "DXT4":
                    return GetBitmapFromDDS_DXT5(data, 128, width, height, true);
                case "DXT5":
                    return GetBitmapFromDDS_DXT5(data, 128, width, height, false);
                case "\0\0\0\0":
                    return GetBitmapFromDDS_RAW(data);
                default:
                    throw new NotImplementedException(String.Format("Unknown DDS FourCC {0:X2}:{1:X2}:{2:X2}:{3:X2}", data[84], data[85], data[86], data[87]));
            }
        }

        public Bitmap GetBitmap()
        {
            byte[] data = Data;
            if (data[0] == 'D' && data[1] == 'D' && data[2] == 'S' && data[3] == ' ')
            {
                return GetBitmapFromDDS(data);
            }
            else
            {
                throw new NotImplementedException(String.Format("Unknown file format {0:X2}:{1:X2}:{2:X2}:{3:X2}", data[0], data[1], data[2], data[3]));
            }
        }

        public void Save()
        {
            string dir = Path.GetDirectoryName(PNGFilename);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            try
            {
                Bitmap bmp = GetBitmap();
                bmp.Save(this.PNGFilename, ImageFormat.Png);
            }
            catch
            {
                File.WriteAllBytes(DDSFilename, Data);
                File.SetLastWriteTimeUtc(DDSFilename, ModTime);
            }
        }

        public static Texture GetTexture(string name)
        {
            if (!Textures.ContainsKey(name))
            {
                Textures[name] = new Texture(name);
            }

            return Textures[name];
        }

        public static void AddTexture(RS5Chunk chunk)
        {
            Texture texture = GetTexture(chunk.Name);
            texture.Chunk = chunk;
        }

        public static void AddTexture(RS5DirectoryEntry dirent)
        {
            Texture texture = GetTexture(dirent.Name);
            texture.Chunk = dirent.Data;
        }

        private Texture(string name)
        {
            this.Name = name;
        }
    }
}

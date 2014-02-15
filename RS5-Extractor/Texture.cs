using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;

namespace RS5_Extractor
{
    public class DDSImage
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int Depth;
        public readonly uint Flags;
        public readonly int Pitch;
        public readonly string FourCC;
        public readonly int RGBBitCount;
        public readonly uint RedMask;
        public readonly uint GreenMask;
        public readonly uint BlueMask;
        public readonly uint AlphaMask;
        public readonly SubStream Data;
        public readonly int ImageOffset;
        public readonly bool IsARGB32;
        public readonly bool HasAlpha;
        public readonly Bitmap Bitmap;
        public readonly double IntensityFactor;
        public readonly string FailureReason;
        public bool HasBitmap { get { return Bitmap != null; } }

        public DDSImage(SubStream data)
        {
            if (data.GetByte(0) == 'D' && data.GetByte(1) == 'D' && data.GetByte(2) == 'S' && data.GetByte(3) == ' ')
            {
                Height = data.GetInt32(12);
                Width = data.GetInt32(16);
                Depth = data.GetInt32(24);
                Flags = data.GetUInt32(8);
                Pitch = data.GetInt32(20);
                FourCC = Encoding.ASCII.GetString(data.GetBytes(84, 4));
                RGBBitCount = data.GetInt32(88);
                RedMask = data.GetUInt32(92);
                GreenMask = data.GetUInt32(96);
                BlueMask = data.GetUInt32(100);
                AlphaMask = data.GetUInt32(104);
                Data = data;
                ImageOffset = 128;

                if (Depth == 0)
                {
                    Depth = 1;
                }

                if ((Flags & 8) == 0)
                {
                    Pitch = Width * RGBBitCount / 8;
                }

                IsARGB32 = (FourCC == "DXT1" || FourCC == "DXT2" || FourCC == "DXT3" || FourCC == "DXT4" || FourCC == "DXT5" || FourCC == "\0\0\0\0");
                IntensityFactor = 1.0;

                try
                {
                    FailureReason = null;
                    switch (FourCC)
                    {
                        case "DXT1":
                            Bitmap = GetBitmapFromDDS_DXT1(Data, ImageOffset, Width, Height, out HasAlpha);
                            break;
                        case "DXT2":
                            Bitmap = GetBitmapFromDDS_DXT3(Data, ImageOffset, Width, Height, true, out HasAlpha);
                            break;
                        case "DXT3":
                            Bitmap = GetBitmapFromDDS_DXT3(Data, ImageOffset, Width, Height, false, out HasAlpha);
                            break;
                        case "DXT4":
                            Bitmap = GetBitmapFromDDS_DXT5(Data, ImageOffset, Width, Height, true, out HasAlpha);
                            break;
                        case "DXT5":
                            Bitmap = GetBitmapFromDDS_DXT5(Data, ImageOffset, Width, Height, false, out HasAlpha);
                            break;
                        case "\0\0\0\0":
                            Bitmap = GetBitmapFromDDS_RAW(Data, ImageOffset, Width, Height, Pitch, RGBBitCount, RedMask, GreenMask, BlueMask, AlphaMask, out HasAlpha, out IsARGB32);
                            break;
                        case "q\0\0\0":
                            Bitmap = GetBitmapFromDDS_ARGB16F(Data, ImageOffset, Width, Height, out HasAlpha, out IntensityFactor);
                            break;
                        default:
                            Bitmap = null;
                            FailureReason = String.Format("Unknown DDS FourCC {0:X2}:{1:X2}:{2:X2}:{3:X2}", Data.GetByte(84), Data.GetByte(85), Data.GetByte(86), Data.GetByte(87));
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Bitmap = null;
                    FailureReason = ex.Message;
                }
            }
            else
            {
                throw new NotImplementedException(String.Format("Unknown file format {0:X2}:{1:X2}:{2:X2}:{3:X2}", data.GetByte(0), data.GetByte(1), data.GetByte(2), data.GetByte(3)));
            }
        }

        #region DDS to Bitmap conversion
        protected static Color[,] GetDXT1ColourBlock(SubStream data, int offset, bool IsDXT3)
        {
            ushort c0v = data.GetUInt16(offset);
            ushort c1v = data.GetUInt16(offset + 2);
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

            ulong lookup = data.GetUInt32(offset + 4);
            Color[,] output = new Color[4, 4];

            for (int i = 0; i < 16; i++)
            {
                output[i % 4, i / 4] = c[(lookup >> (i * 2)) & 3];
            }

            return output;
        }

        protected static Bitmap GetBitmapFromDDS_DXT1(SubStream data, int offset, int width, int height, out bool hasalpha)
        {
            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            byte[] bmpraw = new byte[width * height * 4];
            hasalpha = false;

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
                                bmpraw[i + 3] = colourdata[u, v].A;
                                hasalpha |= colourdata[u, v].A != 255;
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

        protected static Bitmap GetBitmapFromDDS_DXT3(SubStream data, int offset, int width, int height, bool IsDXT2, out bool hasalpha)
        {
            Bitmap bmp = new Bitmap(width, height, IsDXT2 ? PixelFormat.Format32bppPArgb : PixelFormat.Format32bppArgb);
            byte[] bmpraw = new byte[width * height * 4];
            hasalpha = false;

            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    ulong alpharaw = data.GetUInt64(offset);
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
                                hasalpha |= alphadata[u, v] != 255;
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

        protected static Bitmap GetBitmapFromDDS_DXT5(SubStream data, int offset, int width, int height, bool IsDXT4, out bool hasalpha)
        {
            Bitmap bmp = new Bitmap(width, height, IsDXT4 ? PixelFormat.Format32bppPArgb : PixelFormat.Format32bppArgb);
            byte[] bmpraw = new byte[width * height * 4];
            hasalpha = false;

            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    byte[] a = new byte[8];
                    a[0] = data.GetByte(offset);
                    a[1] = data.GetByte(offset + 1);

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

                    ulong alphasel = data.GetUInt64(offset) >> 16;
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
                                hasalpha |= alphadata[u, v] != 255;
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

        protected static int MostSignificantBitPosition(uint v)
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

        protected static int LeastSignificantBitPosition(uint v)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((v & (1 << i)) != 0)
                {
                    return i;
                }
            }
            return -1;
        }

        protected static byte GetMaskShiftVal(uint v, uint mask, int shift, byte valifmaskzero)
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

        protected static Bitmap GetBitmapFromDDS_RAW(SubStream Data, int Offset, int Width, int Height, int Pitch, int RGBBitCount, uint RedMask, uint GreenMask, uint BlueMask, uint AlphaMask, out bool hasalpha, out bool isargb32)
        {
            uint RGBBitMask = (uint)((1L << RGBBitCount) - 1);
            int RedShift = MostSignificantBitPosition(RedMask) - 7;
            int GreenShift = MostSignificantBitPosition(GreenMask) - 7;
            int BlueShift = MostSignificantBitPosition(BlueMask) - 7;
            int AlphaShift = MostSignificantBitPosition(AlphaMask) - 7;
            int RedBits = RedMask == 0 ? 0 : MostSignificantBitPosition(RedMask) - LeastSignificantBitPosition(RedMask) + 1;
            int GreenBits = GreenMask == 0 ? 0 : MostSignificantBitPosition(GreenMask) - LeastSignificantBitPosition(GreenMask) + 1;
            int BlueBits = BlueMask == 0 ? 0 : MostSignificantBitPosition(BlueMask) - LeastSignificantBitPosition(BlueMask) + 1;
            int AlphaBits = AlphaMask == 0 ? 0 : MostSignificantBitPosition(AlphaMask) - LeastSignificantBitPosition(AlphaMask) + 1;
            hasalpha = false;
            isargb32 = RedBits <= 8 && GreenBits <= 8 && BlueBits <= 8 && AlphaBits <= 8;

            Bitmap bmp = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
            byte[] bmpraw = new byte[Width * Height * 4];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int w = (4 - ((RGBBitCount + 7) / 8));
                    int bitpos = y * Pitch * 8 + x * RGBBitCount;
                    int i = ((bitpos + RGBBitCount - 1) / 8 - 7);
                    int b = (bitpos - i * 8);
                    int o = (y * Width + x) * 4;
                    uint v = (uint)((Data.GetUInt64(i + 128) >> b) & RGBBitMask);
                    bmpraw[o + 0] = GetMaskShiftVal(v, BlueMask, BlueShift, 0x00);
                    bmpraw[o + 1] = GetMaskShiftVal(v, GreenMask, GreenShift, 0x00);
                    bmpraw[o + 2] = GetMaskShiftVal(v, RedMask, RedShift, 0x00);
                    bmpraw[o + 3] = GetMaskShiftVal(v, AlphaMask, AlphaShift, 0xFF);
                    hasalpha = bmpraw[o + 3] != 255;
                }
            }
            BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            for (int y = 0; y < Height; y++)
            {
                Marshal.Copy(bmpraw, y * Width * 4, bmpdata.Scan0 + y * bmpdata.Stride, Width * 4);
            }

            bmp.UnlockBits(bmpdata);

            return bmp;
        }

        protected static float HalfToFloat(ushort v)
        {
            uint s = (v & 0x8000U) << 16;
            uint e = ((v & 0x7c00U) >> 10) + 127 - 15;
            uint m = ((v & 0x03ffU) << 13);

            if (e == 127 - 15)
            {
                if (m == 0)
                {
                    e = 0;
                }
                else
                {
                    while ((m & 0x800000) == 0)
                    {
                        m <<= 1;
                        e--;
                    }

                    e++;
                    m &= 0x7fffff;
                }
            }
            else if (e == 127 + 16)
            {
                e = 255;
            }

            uint o = s | (e << 23) | m;
            return BitConverter.ToSingle(BitConverter.GetBytes(o), 0);
        }

        protected static Bitmap GetBitmapFromDDS_ARGB16F(SubStream data, int offset, int width, int height, out bool hasalpha, out double intensity)
        {
            float[] fdata = new float[width * height * 4];
            double maxval = Double.NegativeInfinity;
            double minval = Double.PositiveInfinity;
            double maxalpha = Double.NegativeInfinity;
            double minalpha = Double.PositiveInfinity;
            double alphaoffset = 0.0;
            hasalpha = false;

            for (int i = 0; i < width * height * 4; i+=4)
            {
                for (int j = 0; j < 4; j++)
                {
                    fdata[i + j] = HalfToFloat(data.GetUInt16(offset + (i + j) * 2));
                }

                for (int j = 0; j < 3; j++)
                {
                    if (fdata[i + j] > maxval)
                    {
                        maxval = fdata[i + j];
                    }
                    if (fdata[i + j] < minval)
                    {
                        minval = fdata[i + j];
                    }
                }

                if (fdata[i + 3] > maxalpha)
                {
                    maxalpha = fdata[i + 3];
                }
                if (fdata[i + 3] < minalpha)
                {
                    minalpha = fdata[i + 3];
                }
            }

            if (minval < 0 || minalpha < 0)
            {
                throw new NotImplementedException("ARGB16F with negative values not supported");
            }

            if (maxval == 0)
            {
                maxval = 1.0;
            }

            if (maxalpha == 0)
            {
                maxalpha = 1.0;
                //alphaoffset = 1.0;
            }

            intensity = maxval;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            byte[] bmpraw = new byte[width * height * 4];

            for (int i = 0; i < width * height * 4; i += 4)
            {
                bmpraw[i + 0] = (byte)(fdata[i + 2] * 255 / maxval);
                bmpraw[i + 1] = (byte)(fdata[i + 1] * 255 / maxval);
                bmpraw[i + 2] = (byte)(fdata[i + 0] * 255 / maxval);
                bmpraw[i + 3] = (byte)((fdata[i + 3] + alphaoffset) * 255 / maxalpha);
                hasalpha |= bmpraw[i + 3] != 255;
            }

            BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(bmpraw, y * width * 4, bmpdata.Scan0 + y * bmpdata.Stride, width * 4);
            }

            bmp.UnlockBits(bmpdata);

            return bmp;
        }
        #endregion

    }
    
    public class Texture
    {
        public class TextureRef
        {
            protected WeakReference WeakRef;
            protected Action<Texture> _Mutator;
            protected int? _Width;
            protected int? _Height;
            protected bool? _IsLossless;
            protected bool? _HasAlpha;
            protected bool? _HasBitmap;

            public string Name { get; protected set; }

            public Texture Value
            {
                get
                {
                    Texture value = WeakRef == null ? null : WeakRef.Target as Texture;

                    if (value == null)
                    {
                        value = new Texture(Name);
                        _Mutator(value);

                        value._Width = _Width;
                        value._Height = _Height;
                        value._IsLossless = _IsLossless;
                        value._HasAlpha = _HasAlpha;
                        value._HasBitmap = _HasBitmap;

                        WeakRef = new WeakReference(value);
                    }

                    return value;
                }
            }

            public event Action<Texture> Mutator
            {
                add
                {
                    _Mutator += value;
                }
                remove
                {
                    _Mutator -= value;
                }
            }

            public TextureRef(string name, Action<Texture> mutator)
            {
                this.Name = name;
                this._Mutator = mutator;
            }
        }

        protected static Dictionary<string, TextureRef> Textures = new Dictionary<string, TextureRef>();

        #region Lazy-init property backing
        protected Lazy<DDSImage> _Image;
        protected int? _Width;
        protected int? _Height;
        protected bool? _IsLossless;
        protected bool? _HasAlpha;
        protected bool? _HasBitmap;
        #endregion

        #region Lazy-init properties
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public DDSImage Image { get { return _Image.Value; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Width { get { return (int)(_Width ?? (Image == null ? 0 : Image.Width)); } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Height { get { return (int)(_Height ?? (Image == null ? 0 : Image.Height)); } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool IsLossless { get { return (bool)(_IsLossless ?? (Image == null ? false : Image.IsARGB32)); } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool HasAlpha { get { return (bool)(_HasAlpha ?? (Image == null ? false : Image.HasAlpha)); } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public bool HasBitmap { get { return (bool)(_HasBitmap ?? (Image == null ? false : Image.HasBitmap)); } }
        #endregion

        protected RS5Chunk Chunk;

        public string Name { get; protected set; }
        public DateTime ModTime { get; protected set; }

        public string PNGFilename  { get { return Name + ".png"; } }
        public string JPEGFilename { get { return Name + ".jpg"; } }
        public string DDSFilename  { get { return Name + ".dds"; } }
        public string Filename     { get { return GetFilename(); } }

        #region Lazy-init initializers
        protected DDSImage GetDDS()
        {
            if (Chunk == null)
            {
                return null;
            }
            else
            {
                DDSImage image = new DDSImage(Chunk.Chunks["DATA"].Data);
                _Width = image.Width;
                _Height = image.Height;
                _IsLossless = image.IsARGB32;
                _HasAlpha = image.HasAlpha;
                _HasBitmap = image.HasBitmap;
                return image;
            }
        }

        #endregion

        protected static void SaveBitmap(Bitmap bmp, ImageFormat format, string filename, DateTime ModTime, params EncoderParameter[] encoderparams)
        {
            ImageCodecInfo codec = ImageCodecInfo.GetImageEncoders().Where(c => c.FormatID == format.Guid).Single();

            string dir = Path.GetDirectoryName(filename);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (Stream outfile = File.Create(filename))
            {
                if (encoderparams == null || encoderparams.Length == 0)
                {
                    bmp.Save(outfile, format);
                }
                else
                {
                    bmp.Save(outfile, codec, new EncoderParameters { Param = encoderparams });
                }
            }

            File.SetLastWriteTimeUtc(filename, ModTime);
        }

        public void SaveImage()
        {
            DDSImage image = Image;
            Bitmap bmp = image == null ? null : image.Bitmap;
            string filename = "." + Path.DirectorySeparatorChar + Filename;

            switch (Path.GetExtension(filename))
            {
                case ".dds": SaveDDS(filename, image, ModTime); break;
                case ".png": SavePNG(filename, bmp, ModTime); break;
                case ".jpg": SaveJPEG(filename, bmp, ModTime); break;
                default: throw new InvalidOperationException(String.Format("Unknown extension {0}", Path.GetExtension(filename)));
            }
        }

        public string GetFilename()
        {
            foreach (string name in new string[] { DDSFilename, PNGFilename, JPEGFilename })
            {
                string filename = "." + Path.DirectorySeparatorChar + name;
                if (File.Exists(filename))
                {
                    return filename;
                }
            }

            return (HasBitmap ? (HasAlpha ? PNGFilename : JPEGFilename) : DDSFilename);
        }

        public bool TextureFileExists()
        {
            return File.Exists(GetFilename());
        }

        public void SaveDDS()
        {
            string filename = "." + Path.DirectorySeparatorChar + DDSFilename;
            SaveDDS(filename, Image, ModTime);
        }

        public static void SavePNG(string filename, Bitmap bmp, DateTime ModTime)
        {
            SaveBitmap(bmp, ImageFormat.Png, filename, ModTime);
        }

        public static void SaveJPEG(string filename, Bitmap bmp, DateTime ModTime)
        {
            SaveBitmap(bmp, ImageFormat.Jpeg, filename, ModTime, new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L));
        }

        public static void SaveDDS(string filename, DDSImage image, DateTime ModTime)
        {
            string dir = Path.GetDirectoryName(filename);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (Stream outfile = File.Create(filename))
            {
                image.Data.CopyTo(outfile);
            }
            File.SetLastWriteTimeUtc(filename, ModTime);
        }

        protected static Texture GetTexture(string name, Action<Texture> mutator)
        {
            if (Textures.ContainsKey(name))
            {
                if (mutator != null)
                {
                    Textures[name].Mutator += mutator;
                }
            }
            else
            {
                Textures[name] = new TextureRef(name, mutator ?? ((t) => { }));
            }

            return Textures[name].Value;
        }

        public static Texture GetTexture(string name)
        {
            return GetTexture(name, null);
        }

        public static Texture AddTexture(RS5Chunk chunk)
        {
            return GetTexture(chunk.Name, (t) =>
            {
                t.Chunk = chunk;
            });
        }

        public static Texture AddTexture(RS5DirectoryEntry dirent)
        {
            return GetTexture(dirent.Name, (t) =>
            {
                t.ModTime = dirent.ModTime;
                t.Chunk = dirent.Data;
            });
        }

        private Texture(string name)
        {
            this.Name = name;
            this._Image = new Lazy<DDSImage>(() => GetDDS());
        }
    }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public struct TextureCoordinate
    {
        public readonly Texture Texture;
        public readonly double U;
        public readonly double V;

        public TextureCoordinate(Texture texture, double U, double V)
        {
            this.Texture = texture;
            this.U = U;
            this.V = V;
        }

        public override int GetHashCode()
        {
            return U.GetHashCode() ^ (V.GetHashCode() >> 4) ^ (Texture.GetHashCode() >> 8);
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is TextureCoordinate)
            {
                TextureCoordinate val = (TextureCoordinate)obj;
                return val.U == U && val.V == V && val.Texture == Texture;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return String.Format("<{0},{1}>", U, V);
        }
    }
}

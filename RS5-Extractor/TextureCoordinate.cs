using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public class TextureCoordinate
    {
        public float U;
        public float V;

        public override int GetHashCode()
        {
            return U.GetHashCode() ^ (V.GetHashCode() >> 4);
        }

        public override bool Equals(object obj)
        {
            TextureCoordinate val = obj as TextureCoordinate;
            if (val != null)
            {
                return val.U == U && val.V == V;
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

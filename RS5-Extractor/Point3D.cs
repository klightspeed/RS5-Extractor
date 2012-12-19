using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public class Point3D
    {
        public float X;
        public float Y;
        public float Z;

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ (Y.GetHashCode() >> 4) ^ (Z.GetHashCode() >> 8);
        }

        public override bool Equals(object obj)
        {
            Point3D val = obj as Point3D;
            if (val != null)
            {
                return val.X == X && val.Y == Y && val.Z == Z;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return String.Format("<{0},{1},{2}>", X, Y, Z);
        }
    }
}

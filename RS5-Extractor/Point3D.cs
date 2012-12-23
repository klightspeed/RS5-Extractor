using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public struct Point3D
    {
        public double X;
        public double Y;
        public double Z;

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ (Y.GetHashCode() >> 4) ^ (Z.GetHashCode() >> 8);
        }

        public override bool Equals(object obj)
        {
            if (obj is Point3D)
            {
                Point3D val = (Point3D)obj;
                return val.X == X && val.Y == Y && val.Z == Z;
            }
            else
            {
                return false;
            }
        }

        public static bool operator ==(Point3D a, Point3D b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Point3D a, Point3D b)
        {
            return !a.Equals(b);
        }

        public static Point3D operator +(Point3D a, Point3D b)
        {
            return new Point3D { X = a.X + b.X, Y = a.Y + b.Y, Z = a.Z + b.Z };
        }

        public static Point3D operator -(Point3D a)
        {
            return new Point3D { X = -a.X, Y = -a.Y, Z = -a.Y };
        }

        public static Point3D operator *(Point3D a, double b)
        {
            return new Point3D { X = a.X * b, Y = a.Y * b, Z = a.Z * b };
        }

        public static Point3D operator -(Point3D a, Point3D b)
        {
            return a + (-b);
        }

        public override string ToString()
        {
            return String.Format("<{0},{1},{2}>", X, Y, Z);
        }

        public static Point3D Zero
        {
            get
            {
                return new Point3D { X = 0, Y = 0, Z = 0 };
            }
        }
    }
}

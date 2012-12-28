using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public struct Vector4 : IEnumerable, IEnumerable<double>
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double W { get; set; }
        private int _AddPos;

        public double this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return X;
                    case 1: return Y;
                    case 2: return Z;
                    case 3: return W;
                    default: throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (i)
                {
                    case 0: X = value; break;
                    case 1: Y = value; break;
                    case 2: Z = value; break;
                    case 3: W = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public Vector4(double x, double y, double z, double w)
            : this()
        {
            _AddPos = 4;
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public bool EtaEqual(Vector4 a, double eta)
        {
            double[,] vals = new double[,] { { X, a.X }, {Y, a.Y}, {Z, a.Z} };
            for (int i = 0; i < vals.GetLength(1); i++)
            {
                if ((vals[i,0] < vals[i, 1] * (1.0 - eta)) || (vals[i, 1] < vals[i, 0] * (1.0 - eta)))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(Vector4 a, Vector4 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vector4 a, Vector4 b)
        {
            return !a.Equals(b);
        }

        public static Vector4 operator +(Vector4 a, Vector4 b)
        {
            return new Vector4 { X = a.X + b.X, Y = a.Y + b.Y, Z = a.Z + b.Z, W = a.W + b.W };
        }

        public static Vector4 operator -(Vector4 a)
        {
            return new Vector4 { X = -a.X, Y = -a.Y, Z = -a.Y, W = -a.W };
        }

        public static Vector4 operator *(Vector4 a, double b)
        {
            return new Vector4 { X = a.X * b, Y = a.Y * b, Z = a.Z * b, W = a.W * b };
        }

        public static Vector4 operator *(double a, Vector4 b)
        {
            return b * a;
        }

        public static Vector4 operator /(Vector4 a, double b)
        {
            return a * (1 / b);
        }

        public static Vector4 operator -(Vector4 a, Vector4 b)
        {
            return a + (-b);
        }

        public Vector4 Normalize()
        {
            if (this.W != 0)
            {
                throw new InvalidOperationException("Not a displacement vector");
            }

            double len = Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z);
            return this / len;
        }

        public override string ToString()
        {
            return String.Format("<{0},{1},{2}>", X, Y, Z);
        }

        public static Vector4 Zero
        {
            get
            {
                return new Vector4 { X = 0, Y = 0, Z = 0, W = 0 };
            }
        }

        public void Add(double v)
        {
            this[_AddPos++] = v;
        }

        public IEnumerator<double> GetEnumerator()
        {
            yield return X;
            yield return Y;
            yield return Z;
            yield return W;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

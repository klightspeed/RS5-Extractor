using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public struct Vector4 : IEnumerable, IEnumerable<double>
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;
        public readonly double W;

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
        }

        public Vector4(IEnumerable<double> vals)
            : this(vals.ToArray())
        {
        }

        public Vector4(params double[] array)
        {
            if (array.Length != 4)
            {
                throw new ArgumentException();
            }

            X = array[0];
            Y = array[1];
            Z = array[2];
            W = array[3];
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
            return new Vector4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        }

        public static Vector4 operator -(Vector4 a)
        {
            return new Vector4(-a.X, -a.Y, -a.Y, -a.W);
        }

        public static Vector4 operator *(Vector4 a, double b)
        {
            return new Vector4(a.X * b, a.Y * b, a.Z * b, a.W * b);
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
                return new Vector4(0, 0, 0, 0);
            }
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

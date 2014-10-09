using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibRS5
{
    public struct Matrix4 : IEnumerable, IEnumerable<double>
    {
        private readonly double v11, v12, v13, v14;
        private readonly double v21, v22, v23, v24;
        private readonly double v31, v32, v33, v34;
        private readonly double v41, v42, v43, v44;

        public double this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return v11;
                    case 1: return v12;
                    case 2: return v13;
                    case 3: return v14;
                    case 4: return v21;
                    case 5: return v22;
                    case 6: return v23;
                    case 7: return v24;
                    case 8: return v31;
                    case 9: return v32;
                    case 10: return v33;
                    case 11: return v34;
                    case 12: return v41;
                    case 13: return v42;
                    case 14: return v43;
                    case 15: return v44;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public double this[int j, int i]
        {
            get
            {
                if (i < 0 || i >= 4 || j < 0 || j >= 4)
                {
                    throw new IndexOutOfRangeException();
                }
                return this[j * 4 + i];
            }
        }

        public Matrix4(IEnumerable<double> vals)
            : this(vals.ToArray())
        {
        }

        public Matrix4(params double[] array)
        {
            if (array.Length != 16)
            {
                throw new ArgumentException();
            }

            v11 = array[0];
            v12 = array[1];
            v13 = array[2];
            v14 = array[3];
            v21 = array[4];
            v22 = array[5];
            v23 = array[6];
            v24 = array[7];
            v31 = array[8];
            v32 = array[9];
            v33 = array[10];
            v34 = array[11];
            v41 = array[12];
            v42 = array[13];
            v43 = array[14];
            v44 = array[15];
        }

        public static Matrix4 operator -(Matrix4 mat)
        {
            return mat * -1;
        }
        
        public static Matrix4 operator +(Matrix4 mat1, Matrix4 mat2)
        {
            return new Matrix4(mat1.Zip(mat2, (a, b) => a + b));
        }

        public static Matrix4 operator -(Matrix4 mat1, Matrix4 mat2)
        {
            return mat1 + (-mat2);
        }

        public static Matrix4 operator *(Matrix4 mat1, Matrix4 mat2)
        {
            return new Matrix4(Enumerable.Range(0, 4).SelectMany(j => Enumerable.Range(0, 4).Select(i => mat1[j, 0] * mat2[0, i] + mat1[j, 1] * mat2[1, i] + mat1[j, 2] * mat2[2, i] + mat1[j, 3] * mat2[3, i])));
        }

        public static Vector4 operator *(Matrix4 mat, Vector4 inpoint)
        {
            return new Vector4(Enumerable.Range(0, 4).Select(i => mat[i, 0] * inpoint[0] + mat[i, 1] * inpoint[1] + mat[i, 2] * inpoint[2] + mat[i, 3] * inpoint[3]));
        }

        public static Matrix4 operator *(Matrix4 mat1, double scale)
        {
            return new Matrix4(mat1.Select(v => v * scale));
        }

        public static Matrix4 operator *(double scale, Matrix4 mat1)
        {
            return mat1 * scale;
        }

        public static Matrix4 operator /(Matrix4 a, double scale)
        {
            return a * (1 / scale);
        }

        public static Matrix4 operator /(double scale, Matrix4 a)
        {
            double s0 = a[0, 0] * a[1, 1] - a[1, 0] * a[0, 1];
            double s1 = a[0, 0] * a[1, 2] - a[1, 0] * a[0, 2];
            double s2 = a[0, 0] * a[1, 3] - a[1, 0] * a[0, 3];
            double s3 = a[0, 1] * a[1, 2] - a[1, 1] * a[0, 2];
            double s4 = a[0, 1] * a[1, 3] - a[1, 1] * a[0, 3];
            double s5 = a[0, 2] * a[1, 3] - a[1, 2] * a[0, 3];

            double c5 = a[2, 2] * a[3, 3] - a[3, 2] * a[2, 3];
            double c4 = a[2, 1] * a[3, 3] - a[3, 1] * a[2, 3];
            double c3 = a[2, 1] * a[3, 2] - a[3, 1] * a[2, 2];
            double c2 = a[2, 0] * a[3, 3] - a[3, 0] * a[2, 3];
            double c1 = a[2, 0] * a[3, 2] - a[3, 0] * a[2, 2];
            double c0 = a[2, 0] * a[3, 1] - a[3, 0] * a[2, 1];

            var invdet = scale / (s0 * c5 - s1 * c4 + s2 * c3 + s3 * c2 - s4 * c1 + s5 * c0);

            return new Matrix4(
                (a[1, 1] * c5 - a[1, 2] * c4 + a[1, 3] * c3) * invdet,
                (-a[0, 1] * c5 + a[0, 2] * c4 - a[0, 3] * c3) * invdet,
                (a[3, 1] * s5 - a[3, 2] * s4 + a[3, 3] * s3) * invdet,
                (-a[2, 1] * s5 + a[2, 2] * s4 - a[2, 3] * s3) * invdet,
                (-a[1, 0] * c5 + a[1, 2] * c2 - a[1, 3] * c1) * invdet,
                (a[0, 0] * c5 - a[0, 2] * c2 + a[0, 3] * c1) * invdet,
                (-a[3, 0] * s5 + a[3, 2] * s2 - a[3, 3] * s1) * invdet,
                (a[2, 0] * s5 - a[2, 2] * s2 + a[2, 3] * s1) * invdet,
                (a[1, 0] * c4 - a[1, 1] * c2 + a[1, 3] * c0) * invdet,
                (-a[0, 0] * c4 + a[0, 1] * c2 - a[0, 3] * c0) * invdet,
                (a[3, 0] * s4 - a[3, 1] * s2 + a[3, 3] * s0) * invdet,
                (-a[2, 0] * s4 + a[2, 1] * s2 - a[2, 3] * s0) * invdet,
                (-a[1, 0] * c3 + a[1, 1] * c1 - a[1, 2] * c0) * invdet,
                (a[0, 0] * c3 - a[0, 1] * c1 + a[0, 2] * c0) * invdet,
                (-a[3, 0] * s3 + a[3, 1] * s1 - a[3, 2] * s0) * invdet,
                (a[2, 0] * s3 - a[2, 1] * s1 + a[2, 2] * s0) * invdet
            );
        }

        public static Matrix4 operator /(Matrix4 a, Matrix4 b)
        {
            return a * (1 / b);
        }

        public static bool operator ==(Matrix4 a, Matrix4 b)
        {
            return a.Equals(b);
        }

        public bool Equals(Matrix4 mat)
        {
            return this.Zip(mat, (a, b) => a == b).All(v => v);
        }
        
        public override bool Equals(object obj)
        {
            if (obj != null && obj is Matrix4)
            {
                return Equals((Matrix4)obj);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            int hash = 0;
            double mult = 1;

            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    hash ^= (this[j, i] * mult).GetHashCode();
                    mult *= Math.E;
                }
            }

            return hash;
        }

        public static bool operator !=(Matrix4 a, Matrix4 b)
        {
            return !(a == b);
        }

        public bool EtaEqual(Matrix4 mat, double eta)
        {
            return this.Zip(mat, (a, b) => (a >= b * (1.0 - eta)) && (b >= a * (1.0 - eta))).All(v => v);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<double> GetEnumerator()
        {
            for (int i = 0; i < 16; i++)
            {
                yield return this[i];
            }
        }

        public static readonly Matrix4 Identity = new Matrix4(1, 0, 0, 0,  0, 1, 0, 0,  0, 0, 1, 0,  0, 0, 0, 1);
    }
}

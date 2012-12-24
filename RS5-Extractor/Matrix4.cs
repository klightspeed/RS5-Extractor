using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public struct Matrix4 : IEnumerable, IEnumerable<double>, IEnumerable<double[]>
    {
        private double v11, v12, v13, v14;
        private double v21, v22, v23, v24;
        private double v31, v32, v33, v34;
        private double v41, v42, v43, v44;
        private int _AddPos1;
        private int _AddPos2;

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
                    default: throw new InvalidProgramException();
                }
            }
            set
            {
                switch (i)
                {
                    case 0: v11 = value; break;
                    case 1: v12 = value; break;
                    case 2: v13 = value; break;
                    case 3: v14 = value; break;
                    case 4: v21 = value; break;
                    case 5: v22 = value; break;
                    case 6: v23 = value; break;
                    case 7: v24 = value; break;
                    case 8: v31 = value; break;
                    case 9: v32 = value; break;
                    case 10: v33 = value; break;
                    case 11: v34 = value; break;
                    case 12: v41 = value; break;
                    case 13: v42 = value; break;
                    case 14: v43 = value; break;
                    case 15: v44 = value; break;
                    default: throw new InvalidProgramException();
                }
            }
        }

        public double this[int j,int i]
        {
            get
            {
                if (j < 0 || j >= 4 || i < 0 || i >= 4)
                {
                    throw new IndexOutOfRangeException();
                }
                return this[j * 4 + i];
            }
            set
            {
                if (j < 0 || j >= 4 || i < 0 || i >= 4)
                {
                    throw new IndexOutOfRangeException();
                }
                this[j * 4 + i] = value;
            }
        }

        public Matrix4(double[,] array)
            : this()
        {
            v11 = array[0, 0];
            v12 = array[0, 1];
            v13 = array[0, 2];
            v14 = array[0, 3];
            v21 = array[1, 0];
            v22 = array[1, 1];
            v23 = array[1, 2];
            v24 = array[1, 3];
            v31 = array[2, 0];
            v32 = array[2, 1];
            v33 = array[2, 2];
            v34 = array[2, 3];
            v41 = array[3, 0];
            v42 = array[3, 1];
            v43 = array[3, 2];
            v44 = array[3, 3];
            _AddPos1 = 16;
            _AddPos2 = 4;
        }

        public static Matrix4 operator -(Matrix4 mat)
        {
            return mat * -1;
        }
        
        public static Matrix4 operator +(Matrix4 mat1, Matrix4 mat2)
        {
            Matrix4 mat = new Matrix4();
            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    mat[j, i] = mat1[j, i] + mat2[j, i];
                }
            }
            return mat;
        }

        public static Matrix4 operator -(Matrix4 mat1, Matrix4 mat2)
        {
            return mat1 + (-mat2);
        }

        public static Matrix4 operator *(Matrix4 mat1, Matrix4 mat2)
        {
            Matrix4 mat = new Matrix4();
            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    mat[j,i] = mat1[j,0] * mat2[0,i] + mat1[j,1] * mat2[1,i] + mat1[j,2] * mat2[2,i] + mat1[j,3] * mat2[3,i];
                }
            }
            return mat;
        }

        public static Vector4 operator *(Matrix4 mat, Vector4 inpoint)
        {
            Vector4 outpoint = new Vector4();
            for (int i = 0; i < 4; i++)
            {
                outpoint[i] = mat[i, 0] * inpoint[0] + mat[i, 1] * inpoint[1] + mat[i, 2] * inpoint[2] + mat[i, 3] * inpoint[3];
            }
            return outpoint;
        }

        public static Matrix4 operator *(Matrix4 mat1, double scale)
        {
            Matrix4 mat = new Matrix4();
            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    mat[j, i] = mat1[j, i] * scale;
                }
            }
            return mat;
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

            var invdet = 1.0 / (s0 * c5 - s1 * c4 + s2 * c3 + s3 * c2 - s4 * c1 + s5 * c0);

            Matrix4 b = new Matrix4();

            b[0, 0] = (a[1, 1] * c5 - a[1, 2] * c4 + a[1, 3] * c3) * invdet;
            b[0, 1] = (-a[0, 1] * c5 + a[0, 2] * c4 - a[0, 3] * c3) * invdet;
            b[0, 2] = (a[3, 1] * s5 - a[3, 2] * s4 + a[3, 3] * s3) * invdet;
            b[0, 3] = (-a[2, 1] * s5 + a[2, 2] * s4 - a[2, 3] * s3) * invdet;

            b[1, 0] = (-a[1, 0] * c5 + a[1, 2] * c2 - a[1, 3] * c1) * invdet;
            b[1, 1] = (a[0, 0] * c5 - a[0, 2] * c2 + a[0, 3] * c1) * invdet;
            b[1, 2] = (-a[3, 0] * s5 + a[3, 2] * s2 - a[3, 3] * s1) * invdet;
            b[1, 3] = (a[2, 0] * s5 - a[2, 2] * s2 + a[2, 3] * s1) * invdet;

            b[2, 0] = (a[1, 0] * c4 - a[1, 1] * c2 + a[1, 3] * c0) * invdet;
            b[2, 1] = (-a[0, 0] * c4 + a[0, 1] * c2 - a[0, 3] * c0) * invdet;
            b[2, 2] = (a[3, 0] * s4 - a[3, 1] * s2 + a[3, 3] * s0) * invdet;
            b[2, 3] = (-a[2, 0] * s4 + a[2, 1] * s2 - a[2, 3] * s0) * invdet;

            b[3, 0] = (-a[1, 0] * c3 + a[1, 1] * c1 - a[1, 2] * c0) * invdet;
            b[3, 1] = (a[0, 0] * c3 - a[0, 1] * c1 + a[0, 2] * c0) * invdet;
            b[3, 2] = (-a[3, 0] * s3 + a[3, 1] * s1 - a[3, 2] * s0) * invdet;
            b[3, 3] = (a[2, 0] * s3 - a[2, 1] * s1 + a[2, 2] * s0) * invdet;

            return b * scale;
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
            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (this[j, i] != mat[j, i])
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is Matrix4))
            {
                return false;
            }

            return Equals((Matrix4)obj);
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

        public bool EtaEqual(Matrix4 a, double eta)
        {
            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    if ((this[j, i] < a[j, i] * (1.0 - eta)) || (a[j, i] < this[j, i] * (1.0 - eta)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void Add(double v)
        {
            if (_AddPos2 != 0)
            {
                throw new InvalidOperationException("Mixed calls to Add(v) and Add(a,b,c,d)");
            }
            this[_AddPos1++] = v;
        }

        public void Add(double a, double b, double c, double d)
        {
            if (_AddPos1 != 0)
            {
                throw new InvalidOperationException("Mixed calls to Add(v) and Add(a,b,c,d)");
            }
            this[_AddPos2, 0] = a;
            this[_AddPos2, 1] = b;
            this[_AddPos2, 2] = c;
            this[_AddPos2, 3] = d;
            _AddPos2++;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<double> IEnumerable<double>.GetEnumerator()
        {
            foreach (double[] vals in this)
            {
                foreach (double val in vals)
                {
                    yield return val;
                }
            }
        }

        public IEnumerator<double[]> GetEnumerator()
        {
            for (int i = 0; i < 4; i++)
            {
                yield return new double[] { this[i,0], this[i,1], this[i,2], this[i,3] };
            }
        }

        public static Matrix4 Identity
        {
            get
            {
                return new Matrix4
                {
                    { 1, 0, 0, 0 },
                    { 0, 1, 0, 0 },
                    { 0, 0, 1, 0 },
                    { 0, 0, 0, 1 }
                };
            }
        }
    }
}

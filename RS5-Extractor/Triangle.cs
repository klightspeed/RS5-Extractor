using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public class Triangle : IEnumerable, IEnumerable<Vertex>
    {
        public Vertex A { get; set; }
        public Vertex B { get; set; }
        public Vertex C { get; set; }
        public Texture Texture { get; set; }
        private int _AddPos;

        public Vertex this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return A;
                    case 1: return B;
                    case 2: return C;
                    default: throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (i)
                {
                    case 0: A = value; break;
                    case 1: B = value; break;
                    case 2: C = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public void Add(Vertex v)
        {
            this[_AddPos++] = v;
        }

        public IEnumerator<Vertex> GetEnumerator()
        {
            yield return A;
            yield return B;
            yield return C;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

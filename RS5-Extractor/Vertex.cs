using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace RS5_Extractor
{
    public class Vertex
    {
        public int Index;
        public Point3D Position;
        public TextureCoordinate TexCoord;
        public Color Diffuse;
        public Color Ambient;
        public Color Specular;
        public Texture Texture;
    }
}

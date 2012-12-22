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
        public Texture Texture;
        public JointInfluence[] JointInfluence;
        public Point3D Normal;
        public Point3D Tangent;
        public Point3D Binormal;
        public byte[] ExtraData;
    }
}

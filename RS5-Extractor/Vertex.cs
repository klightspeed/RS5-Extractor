using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace RS5_Extractor
{
    public class Vertex
    {
        public Vector4 Position;
        public Vector4 Normal;
        public Vector4 Tangent;
        public Vector4 Binormal;
        public TextureCoordinate TexCoord;
        public JointInfluence[] JointInfluence;
        public byte[] ExtraData;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace RS5_Extractor
{
    public class Vertex
    {
        public Vector4 Position { get; set; }
        public Vector4 Normal { get; set; }
        public Vector4 Tangent { get; set; }
        public Vector4 Binormal { get; set; }
        public TextureCoordinate TexCoord { get; set; }
        public JointInfluence[] JointInfluence { get; set; }
        public byte[] ExtraData { get; set; }
    }
}

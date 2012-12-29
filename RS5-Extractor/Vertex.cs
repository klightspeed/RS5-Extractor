using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace RS5_Extractor
{
    public class Vertex
    {
        public readonly Vector4 Position;
        public readonly Vector4 Normal;
        public readonly Vector4 Tangent;
        public readonly Vector4 Binormal;
        public readonly TextureCoordinate TexCoord;
        public readonly JointInfluence[] JointInfluence;
        public readonly byte[] ExtraData;

        public Vertex(Vector4 position, Vector4 normal, Vector4 tangent, Vector4 binormal, TextureCoordinate texcoord, IEnumerable<JointInfluence> jointinfluence, byte[] extradata)
        {
            this.Position = position;
            this.Normal = normal;
            this.Tangent = tangent;
            this.Binormal = binormal;
            this.TexCoord = texcoord;
            this.JointInfluence = jointinfluence == null ? new JointInfluence[0] : jointinfluence.ToArray();
            this.ExtraData = extradata == null ? new byte[0] : extradata;
        }
    }
}

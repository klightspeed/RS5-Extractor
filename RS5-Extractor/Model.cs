using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Drawing;
using System.IO;

namespace RS5_Extractor
{
    public abstract class Model
    {
        protected RS5Chunk Chunk;

        private List<Vertex> _Vertices;
        public List<Vertex> Vertices
        {
            get
            {
                if (_Vertices == null)
                {
                    FillModel();
                }
                return _Vertices;
            }
        }

        private List<Triangle> _Triangles;
        public List<Triangle> Triangles
        {
            get
            {
                if (_Triangles == null)
                {
                    FillModel();
                }
                return _Triangles;
            }
        }

        private Dictionary<string,Texture> _Textures;
        public Dictionary<string,Texture> Textures
        {
            get
            {
                if (_Textures == null)
                {
                    FillModel();
                }
                return _Textures;
            }
        }

        private Dictionary<string, Joint> _Joints;
        public Dictionary<string, Joint> Joints
        {
            get
            {
                if (_Joints == null)
                {
                    FillModel();
                }
                return _Joints;
            }
        }

        private Dictionary<string, AnimationSequence> _Animations;
        public Dictionary<string, AnimationSequence> Animations
        {
            get
            {
                if (_Animations == null)
                {
                    FillModel();
                }
                return _Animations;
            }
        }

        public Joint RootJoint
        {
            get
            {
                return Joints.Values.Where(j => j.Parent == null).SingleOrDefault();
            }
        }

        private byte[] _ExtraData;
        public byte[] ExtraData
        {
            get
            {
                if (_ExtraData == null)
                {
                    FillModel();
                }
                return _ExtraData;
            }
            protected set
            {
                _ExtraData = value;
            }
        }

        public string Name { get; private set; }
        public DateTime ModTime { get; private set; }

        protected Model(RS5DirectoryEntry dirent)
        {
            this.Chunk = dirent.Data;
            this.Name = dirent.Name;
            this.ModTime = dirent.ModTime;
        }

        public bool IsAnimated
        {
            get
            {
                return Animations.Count != 0;
            }
        }

        public int NumAnimationFrames
        {
            get
            {
                return IsAnimated ? Animations.First().Value.Frames.Count : 0;
            }
        }

        public bool HasMultipleTextures
        {
            get
            {
                return Textures.Count() > 1;
            }
        }

        public bool HasGeometry
        {
            get
            {
                return Vertices.Count != 0;
            }
        }

        public bool HasNormals
        {
            get
            {
                return HasGeometry && Vertices.First().Normal != Vector4.Zero;
            }
        }

        public bool HasTangents
        {
            get
            {
                return HasGeometry && Vertices.First().Tangent != Vector4.Zero;
            }
        }

        public bool HasBinormals
        {
            get
            {
                return HasGeometry && Vertices.First().Binormal != Vector4.Zero;
            }
        }

        public bool HasSkeleton
        {
            get
            {
                return RootJoint != null;
            }
        }
        
        public string ColladaFilename
        {
            get
            {
                return ".\\" + Name + ".dae";
            }
        }


        protected string[] ExludeTexturePrefixes = new string[]
        {
            "TEX\\CX",
            "TEX\\CCX",
            "TEX\\LX",
            "TEX\\SPECIAL"
        };

        protected void Save(string filename, bool skin, int startframe, int numframes, float framerate)
        {
            string dir = Path.GetDirectoryName(filename);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (Stream stream = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true }))
                {
                    WriteColladaXml(writer, skin, startframe, numframes, framerate);
                }
            }

            File.SetLastWriteTimeUtc(filename, ModTime);
        }

        public void Save()
        {
            Save(ColladaFilename, false, 0, 0, 0);
        }

        public void SaveAnimation(string animname, int startframe, int numframes, float framerate)
        {
            string filename = ".\\" + Name + ".anim." + animname + ".dae";
            Save(filename, true, startframe, numframes, framerate);
        }

        public void SaveUnanimated()
        {
            string filename = ".\\" + Name + ".noanim.dae";
            Save(filename, true, 0, 0, 0);
        }

        protected AnimationSequence TrimAnimationSequence(AnimationSequence sequence, int startframe, int numframes)
        {
            int incr = 1;
            if (numframes < 0)
            {
                incr = -1;
                numframes = -numframes;
            }

            AnimationSequence outseq = new AnimationSequence();
            outseq.Frames[0] = sequence.Frames[startframe];
            
            for (int i = 1; i < numframes - 1; i++)
            {
                int index = startframe + i * incr;
                if (sequence.Frames[index - 1] != sequence.Frames[index] || sequence.Frames[index + 1] != sequence.Frames[index])
                {
                    if (!sequence.Frames[index].EtaEqual((sequence.Frames[index - 1] + sequence.Frames[index + 1]) * 0.5, 1.0 / 1048576))
                    {
                        outseq.Frames[i] = sequence.Frames[index];
                    }
                }
            }

            outseq.Frames[numframes - 1] = sequence.Frames[startframe + (numframes - 1) * incr];

            return outseq;
        }

        protected void WriteColladaXmlAnimation(XmlWriter writer, string animationname, string skeletonname, Dictionary<string, AnimationSequence> sequences, int startframe, int numframes, float framerate)
        {
            writer.WriteStartElement("animation");
            foreach (KeyValuePair<string, AnimationSequence> sequence_kvp in sequences)
            {
                AnimationSequence sequence = TrimAnimationSequence(sequence_kvp.Value, startframe, numframes);
                string seqname = sequence_kvp.Key;
                string seqid = animationname + "_" + seqname;

                writer.WriteStartElement("animation");
                writer.WriteAttributeString("id", seqid);
                writer.WriteAttributeString("name", seqname);
                writer.WriteStartElement("source");
                writer.WriteAttributeString("id", seqid + "_time");
                writer.WriteStartElement("float_array");
                writer.WriteAttributeString("id", seqid + "_time_array");
                writer.WriteAttributeString("count", sequence.Frames.Count.ToString());
                
                foreach (KeyValuePair<int, Matrix4> frame in sequence.Frames)
                {
                    writer.WriteString(String.Format("{0:F3} ", frame.Key / framerate));
                }
                
                writer.WriteEndElement(); // float_array
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString("source", "#" + seqid + "_time_array");
                writer.WriteAttributeString("count", sequence.Frames.Count.ToString());
                writer.WriteAttributeString("stride", "1");
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "TIME");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement(); // param
                writer.WriteEndElement(); // accessor
                writer.WriteEndElement(); // technique_common
                writer.WriteEndElement(); // source
                writer.WriteStartElement("source");
                writer.WriteAttributeString("id", seqid + "_out_xfrm");
                writer.WriteStartElement("float_array");
                writer.WriteAttributeString("id", seqid + "_out_xfrm_array");
                writer.WriteAttributeString("count", (sequence.Frames.Count * 16).ToString());
                
                foreach (KeyValuePair<int, Matrix4> frame in sequence.Frames)
                {
                    writer.WriteWhitespace("\n");
                    for (int j = 0; j < 4; j++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            writer.WriteString(String.Format("{0,8:F5} ", frame.Value[j, i]));
                        }
                    }
                }

                writer.WriteWhitespace("\n");
                writer.WriteEndElement(); // float_array
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString("source", "#" + seqid + "_out_xfrm_array");
                writer.WriteAttributeString("count", sequence.Frames.Count.ToString());
                writer.WriteAttributeString("stride", "16");
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "TRANSFORM");
                writer.WriteAttributeString("type", "float4x4");
                writer.WriteEndElement(); // param
                writer.WriteEndElement(); // accessor
                writer.WriteEndElement(); // technique_common
                writer.WriteEndElement(); // source
                writer.WriteStartElement("source");
                writer.WriteAttributeString("id", seqid + "_interp");
                writer.WriteStartElement("Name_array");
                writer.WriteAttributeString("id", seqid + "_interp_array");
                writer.WriteAttributeString("count", sequence.Frames.Count.ToString());

                for (int i = 0; i < sequence.Frames.Count; i++)
                {
                    writer.WriteString("LINEAR ");
                }

                writer.WriteEndElement(); // Name_array
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString("source", "#" + seqid + "_interp_array");
                writer.WriteAttributeString("count", sequence.Frames.Count.ToString());
                writer.WriteAttributeString("stride", "1");
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "INTERPOLATION");
                writer.WriteAttributeString("type", "name");
                writer.WriteEndElement(); // param
                writer.WriteEndElement(); // accessor
                writer.WriteEndElement(); // technique_common
                writer.WriteEndElement(); // source
                writer.WriteStartElement("sampler");
                writer.WriteAttributeString("id", seqid + "_xfrm");
                writer.WriteStartElement("input");
                writer.WriteAttributeString("semantic", "INPUT");
                writer.WriteAttributeString("source", "#" + seqid + "_time");
                writer.WriteEndElement(); // input
                writer.WriteStartElement("input");
                writer.WriteAttributeString("semantic", "OUTPUT");
                writer.WriteAttributeString("source", "#" + seqid + "_out_xfrm");
                writer.WriteEndElement(); // input
                writer.WriteStartElement("input");
                writer.WriteAttributeString("semantic", "INTERPOLATION");
                writer.WriteAttributeString("source", "#" + seqid + "_interp");
                writer.WriteEndElement(); // input
                writer.WriteEndElement(); // sampler
                writer.WriteStartElement("channel");
                writer.WriteAttributeString("source", "#" + seqid + "_xfrm");
                writer.WriteAttributeString("target", skeletonname + "_" + seqname + "/transform.MATRIX");
                writer.WriteEndElement(); // channel
                writer.WriteEndElement(); // animation
            }
            writer.WriteEndElement(); // animation
        }

        protected void WriteColladaXmlJointsSkin(XmlWriter writer, string geometryname, string skeletonname, string skinname, List<Vertex> vertices, List<Joint> joints)
        {
            writer.WriteStartElement("controller");
            writer.WriteAttributeString("id", skinname);
            writer.WriteAttributeString("name", skinname);
            writer.WriteStartElement("skin");
            writer.WriteAttributeString("source", "#" + geometryname);
            writer.WriteElementString("bind_shape_matrix", "1 0 0 0  0 1 0 0  0 0 1 0  0 0 0 1");
            writer.WriteStartElement("source");
            writer.WriteAttributeString("id", skinname + "_jnt");
            writer.WriteStartElement("IDREF_array");
            writer.WriteAttributeString("id", skinname + "_jnt_array");
            writer.WriteAttributeString("count", joints.Count.ToString());
            foreach (Joint joint in joints)
            {
                writer.WriteString(skeletonname + "_" + joint.Symbol + " ");
            }
            writer.WriteEndElement(); // Name_array
            writer.WriteStartElement("technique_common");
            writer.WriteStartElement("accessor");
            writer.WriteAttributeString("source", "#" + skinname + "_jnt_array");
            writer.WriteAttributeString("count", joints.Count.ToString());
            writer.WriteAttributeString("stride", "1");
            writer.WriteStartElement("param");
            writer.WriteAttributeString("name", "JOINT");
            writer.WriteAttributeString("type", "IDREF");
            writer.WriteEndElement(); // param
            writer.WriteEndElement(); // accessor
            writer.WriteEndElement(); // technique_common
            writer.WriteEndElement(); // source
            writer.WriteStartElement("source");
            writer.WriteAttributeString("id", skinname + "_bnd");
            writer.WriteStartElement("float_array");
            writer.WriteAttributeString("id", skinname + "_bnd_array");
            writer.WriteAttributeString("count", (joints.Count * 16).ToString());
            foreach (Joint joint in joints)
            {
                writer.WriteWhitespace("\n");
                for (int j = 0; j < 4; j++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        writer.WriteString(String.Format("{0,8:F5} ", joint.ReverseBindingMatrix[j, i]));
                    }
                }
            }
            writer.WriteWhitespace("\n");
            writer.WriteEndElement(); // float_array
            writer.WriteStartElement("technique_common");
            writer.WriteStartElement("accessor");
            writer.WriteAttributeString("source", "#" + skinname + "_bnd_array");
            writer.WriteAttributeString("count", joints.Count.ToString());
            writer.WriteAttributeString("stride", "16");
            writer.WriteStartElement("param");
            writer.WriteAttributeString("name", "TRANSFORM");
            writer.WriteAttributeString("type", "float4x4");
            writer.WriteEndElement(); // param
            writer.WriteEndElement(); // accessor
            writer.WriteEndElement(); // technique_common
            writer.WriteEndElement(); // source
            writer.WriteStartElement("source");
            writer.WriteAttributeString("id", skinname + "_wgt");
            writer.WriteStartElement("float_array");
            writer.WriteAttributeString("id", skinname + "_wgt_array");
            writer.WriteAttributeString("count", "256");
            for (int i = 0; i < 256; i++)
            {
                writer.WriteString(String.Format("{0,6:F4} ", i / 255.0));
            }
            writer.WriteEndElement(); //float_array
            writer.WriteStartElement("technique_common");
            writer.WriteStartElement("accessor");
            writer.WriteAttributeString("source", "#" + skinname + "_wgt_array");
            writer.WriteAttributeString("count", "256");
            writer.WriteAttributeString("stride", "1");
            writer.WriteStartElement("param");
            writer.WriteAttributeString("name", "WEIGHT");
            writer.WriteAttributeString("type", "float");
            writer.WriteEndElement(); // param
            writer.WriteEndElement(); // accessor
            writer.WriteEndElement(); // technique_common
            writer.WriteEndElement(); //source
            writer.WriteStartElement("joints");
            writer.WriteStartElement("input");
            writer.WriteAttributeString("semantic", "JOINT");
            writer.WriteAttributeString("source", "#" + skinname + "_jnt");
            writer.WriteEndElement(); // input
            writer.WriteStartElement("input");
            writer.WriteAttributeString("semantic", "INV_BIND_MATRIX");
            writer.WriteAttributeString("source", "#" + skinname + "_bnd");
            writer.WriteEndElement(); // input
            writer.WriteEndElement(); // joints
            writer.WriteStartElement("vertex_weights");
            writer.WriteAttributeString("count", vertices.SelectMany(v => v.JointInfluence).Count().ToString());
            writer.WriteStartElement("input");
            writer.WriteAttributeString("offset", "0");
            writer.WriteAttributeString("semantic", "JOINT");
            writer.WriteAttributeString("source", "#" + skinname + "_jnt");
            writer.WriteEndElement(); // input
            writer.WriteStartElement("input");
            writer.WriteAttributeString("offset", "1");
            writer.WriteAttributeString("semantic", "WEIGHT");
            writer.WriteAttributeString("source", "#" + skinname + "_wgt");
            writer.WriteEndElement(); // input
            writer.WriteStartElement("vcount");
            foreach (Vertex vertex in vertices)
            {
                writer.WriteString(String.Format("{0} ", vertex.JointInfluence.Length));
            }
            writer.WriteEndElement(); // vcount
            writer.WriteStartElement("v");
            foreach (Vertex vertex in vertices)
            {
                writer.WriteWhitespace("\n");
                foreach (JointInfluence influence in vertex.JointInfluence)
                {
                    writer.WriteString(String.Format("{0,3} {1,3}  ", influence.JointIndex, (int)((influence.Influence * 255.0) + 0.25)));
                }
            }
            writer.WriteWhitespace("\n");
            writer.WriteEndElement(); // v
            writer.WriteEndElement(); // vertex_weights
            writer.WriteEndElement(); // skin
            writer.WriteEndElement(); // controller
        }

        protected void WriteColladaXmlSkeleton(XmlWriter writer, string skeletonname, Joint joint, Matrix4 parentmatrix)
        {
            if (joint.InitialPose == null)
            {
                if (parentmatrix[3, 0] != 0 || parentmatrix[3, 1] != 0 || parentmatrix[3, 2] != 0 || parentmatrix[3, 3] != 1)
                {
                    throw new InvalidDataException("parent matrix not a transformation matrix");
                }

                Matrix4 relmatrix = parentmatrix / joint.ReverseBindingMatrix;
                joint.InitialPose = relmatrix;
            }

            writer.WriteStartElement("node");
            writer.WriteAttributeString("id", skeletonname + "_" + joint.Symbol);
            writer.WriteAttributeString("name", joint.Name);
            writer.WriteAttributeString("sid", joint.Symbol);
            writer.WriteAttributeString("type", "JOINT");
            writer.WriteStartElement("matrix");
            writer.WriteAttributeString("sid", "transform");
            for (int j = 0; j < 4; j++)
            {
                for (int i = 0; i < 4; i++)
                {
                    writer.WriteString(String.Format("{0,8:F5} ", joint.InitialPose[j,i]));
                }
            }
            writer.WriteEndElement(); // matrix
            foreach (Joint childjoint in joint.Children)
            {
                WriteColladaXmlSkeleton(writer, skeletonname, childjoint, joint.ReverseBindingMatrix);
            }
            writer.WriteEndElement(); // node
        }

        protected void WriteColladaXmlGeometryInstance(XmlWriter writer, string modelname, string geometryname, string skinname, string skeletonname, Dictionary<string, Texture> textures)
        {
            bool visible = false;
            writer.WriteStartElement("node");
            writer.WriteAttributeString("id", modelname);
            if (skeletonname != null)
            {
                writer.WriteStartElement("instance_controller");
                writer.WriteAttributeString("url", "#" + skinname);
                writer.WriteElementString("skeleton", "#" + skeletonname);
            }
            else
            {
                writer.WriteStartElement("instance_geometry");
                writer.WriteAttributeString("url", "#" + geometryname);
            }
            int texnum = 0;
            foreach (KeyValuePair<string, Texture> texture_kvp in textures)
            {
                string texname = texture_kvp.Key;
                Texture texture = texture_kvp.Value;
                visible |= ExludeTexturePrefixes.Count(v => texture.Name.StartsWith(v)) == 0;
                writer.WriteStartElement("bind_material");
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("instance_material");
                writer.WriteAttributeString("symbol", texname + "_lnk");
                writer.WriteAttributeString("target", "#" + texname + "_mtl");
                writer.WriteStartElement("bind_vertex_input");
                writer.WriteAttributeString("semantic", "TEXCOORD");
                writer.WriteAttributeString("input_semantic", "TEXCOORD");
                writer.WriteAttributeString("input_set", String.Format("{0}", texnum));
                writer.WriteEndElement(); // bind_vertex_input
                writer.WriteEndElement(); // instance_material
                writer.WriteEndElement(); // technique_common
                writer.WriteEndElement(); // bind_material
                texnum++;

            }
            writer.WriteEndElement(); // instance_geometry|instance_controller
            writer.WriteStartElement("extra");
            writer.WriteStartElement("technique");
            writer.WriteAttributeString("profile", "FCOLLADA");
            writer.WriteElementString("visibility", visible ? "1" : "0");
            writer.WriteEndElement(); // technique
            writer.WriteStartElement("technique");
            writer.WriteAttributeString("profile", "XSI");
            writer.WriteStartElement("SI_Visibility");
            writer.WriteStartElement("xsi_param");
            writer.WriteAttributeString("sid", "visibility");
            writer.WriteString(visible ? "TRUE" : "FALSE");
            writer.WriteEndElement(); // xsi_param
            writer.WriteEndElement(); // SI_Visibility
            writer.WriteEndElement(); // technique
            writer.WriteEndElement(); // extra
            writer.WriteEndElement(); // node
        }
        
        protected List<Vertex> WriteColladaXmlGeometry(XmlWriter writer, string geometryname, List<Triangle> triangles, Dictionary<string,Texture> textures)
        {
            List<Vertex> vertices = new List<Vertex>();

            foreach (Triangle triangle in triangles)
            {
                foreach (Vertex vertex in new Vertex[] { triangle.A, triangle.B, triangle.C })
                {
                    if (vertex.Index < 0 || vertices.Count <= vertex.Index || vertices[vertex.Index] != vertex)
                    {
                        vertex.Index = vertices.Count;
                        vertices.Add(vertex);
                    }
                }
            }
            
            writer.WriteStartElement("geometry");
            writer.WriteAttributeString("id", geometryname);
            writer.WriteStartElement("mesh");
            writer.WriteStartElement("source");
            writer.WriteAttributeString("id", geometryname + "_pos");
            writer.WriteStartElement("float_array");
            writer.WriteAttributeString("id", geometryname + "_pos_array");

            writer.WriteAttributeString("count", (vertices.Count * 3).ToString());
            foreach (Vertex vertex in vertices)
            {
                writer.WriteWhitespace("\n");
                writer.WriteString(String.Format("{0,12:F6} {1,12:F6} {2,12:F6} ", vertex.Position.X, vertex.Position.Y, vertex.Position.Z));
                /*
                if (vertex.ExtraData != null)
                {
                    writer.WriteComment(String.Join(" ", vertex.ExtraData.Select(b => String.Format("{0:X2}", b))));
                }
                 */
            }
            writer.WriteWhitespace("\n");
            writer.WriteEndElement(); // float_array
            writer.WriteStartElement("technique_common");
            writer.WriteStartElement("accessor");
            writer.WriteAttributeString("count", vertices.Count.ToString());
            writer.WriteAttributeString("offset", "0");
            writer.WriteAttributeString("source", "#" + geometryname + "_pos_array");
            writer.WriteAttributeString("stride", "3");
            writer.WriteStartElement("param");
            writer.WriteAttributeString("name", "X");
            writer.WriteAttributeString("type", "float");
            writer.WriteEndElement(); // param
            writer.WriteStartElement("param");
            writer.WriteAttributeString("name", "Y");
            writer.WriteAttributeString("type", "float");
            writer.WriteEndElement(); // param
            writer.WriteStartElement("param");
            writer.WriteAttributeString("name", "Z");
            writer.WriteAttributeString("type", "float");
            writer.WriteEndElement(); // param
            writer.WriteEndElement(); // accessor
            writer.WriteEndElement(); // technique_common
            writer.WriteEndElement(); // source
            writer.WriteStartElement("source");
            writer.WriteAttributeString("id", geometryname + "_tex");
            writer.WriteStartElement("float_array");
            writer.WriteAttributeString("id", geometryname + "_tex_array");
            writer.WriteAttributeString("count", (vertices.Count * 2).ToString());
            foreach (Vertex vertex in vertices)
            {
                writer.WriteWhitespace("\n");
                writer.WriteString(String.Format("{0,8:F5} {1,8:F5}", vertex.TexCoord.U, vertex.TexCoord.V));
            }
            writer.WriteWhitespace("\n");
            writer.WriteEndElement(); // float_array
            writer.WriteStartElement("technique_common");
            writer.WriteStartElement("accessor");
            writer.WriteAttributeString("count", vertices.Count.ToString());
            writer.WriteAttributeString("offset", "0");
            writer.WriteAttributeString("source", "#" + geometryname + "_tex_array");
            writer.WriteAttributeString("stride", "2");
            writer.WriteStartElement("param");
            writer.WriteAttributeString("name", "S");
            writer.WriteAttributeString("type", "float");
            writer.WriteEndElement(); // param
            writer.WriteStartElement("param");
            writer.WriteAttributeString("name", "T");
            writer.WriteAttributeString("type", "float");
            writer.WriteEndElement(); // param
            writer.WriteEndElement(); // accessor
            writer.WriteEndElement(); // technique_common
            writer.WriteEndElement(); // source
            if (HasNormals)
            {
                writer.WriteStartElement("source");
                writer.WriteAttributeString("id", geometryname + "_normal");
                writer.WriteStartElement("float_array");
                writer.WriteAttributeString("id", geometryname + "_normal_array");
                writer.WriteAttributeString("count", (vertices.Count * 3).ToString());
                foreach (Vertex vertex in vertices)
                {
                    writer.WriteWhitespace("\n");
                    writer.WriteString(String.Format("{0:F3} {1:F3} {2:F3}", vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z));
                }
                writer.WriteWhitespace("\n");
                writer.WriteEndElement(); // float_array
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString("count", vertices.Count.ToString());
                writer.WriteAttributeString("offset", "0");
                writer.WriteAttributeString("source", "#" + geometryname + "_normal_array");
                writer.WriteAttributeString("stride", "3");
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "X");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement(); // param
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "Y");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement(); // param
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "Z");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement(); // param
                writer.WriteEndElement(); // accessor
                writer.WriteEndElement(); // technique_common
                writer.WriteEndElement(); // source
            }
            if (HasTangents)
            {
                writer.WriteStartElement("source");
                writer.WriteAttributeString("id", geometryname + "_tangent");
                writer.WriteStartElement("float_array");
                writer.WriteAttributeString("id", geometryname + "_tangent_array");
                writer.WriteAttributeString("count", (vertices.Count * 3).ToString());
                foreach (Vertex vertex in vertices)
                {
                    writer.WriteWhitespace("\n");
                    writer.WriteString(String.Format("{0:F3} {1:F3} {2:F3}", vertex.Tangent.X, vertex.Tangent.Y, vertex.Tangent.Z));
                }
                writer.WriteWhitespace("\n");
                writer.WriteEndElement(); // float_array
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString("count", vertices.Count.ToString());
                writer.WriteAttributeString("offset", "0");
                writer.WriteAttributeString("source", "#" + geometryname + "_tangent_array");
                writer.WriteAttributeString("stride", "3");
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "X");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement(); // param
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "Y");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement(); // param
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "Z");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement(); // param
                writer.WriteEndElement(); // accessor
                writer.WriteEndElement(); // technique_common
                writer.WriteEndElement(); // source
            }
            if (HasBinormals)
            {
                writer.WriteStartElement("source");
                writer.WriteAttributeString("id", geometryname + "_binormal");
                writer.WriteStartElement("float_array");
                writer.WriteAttributeString("id", geometryname + "_binormal_array");
                writer.WriteAttributeString("count", (vertices.Count * 3).ToString());
                foreach (Vertex vertex in vertices)
                {
                    writer.WriteWhitespace("\n");
                    writer.WriteString(String.Format("{0:F3} {1:F3} {2:F3}", vertex.Binormal.X, vertex.Binormal.Y, vertex.Binormal.Z));
                }
                writer.WriteWhitespace("\n");
                writer.WriteEndElement(); // float_array
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString("count", vertices.Count.ToString());
                writer.WriteAttributeString("offset", "0");
                writer.WriteAttributeString("source", "#" + geometryname + "_binormal_array");
                writer.WriteAttributeString("stride", "3");
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "X");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement(); // param
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "Y");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement(); // param
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "Z");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement(); // param
                writer.WriteEndElement(); // accessor
                writer.WriteEndElement(); // technique_common
                writer.WriteEndElement(); // source
            }
            writer.WriteStartElement("vertices");
            writer.WriteAttributeString("id", geometryname + "_vtx");
            writer.WriteStartElement("input");
            writer.WriteAttributeString("semantic", "POSITION");
            writer.WriteAttributeString("source", "#" + geometryname + "_pos");
            writer.WriteEndElement(); // input
            if (HasNormals)
            {
                writer.WriteStartElement("input");
                writer.WriteAttributeString("semantic", "NORMAL");
                writer.WriteAttributeString("source", "#" + geometryname + "_normal");
                writer.WriteEndElement(); // input
            }
            if (HasTangents)
            {
                writer.WriteStartElement("input");
                writer.WriteAttributeString("semantic", "TANGENT");
                writer.WriteAttributeString("source", "#" + geometryname + "_tangent");
                writer.WriteEndElement(); // input
            }
            if (HasBinormals)
            {
                writer.WriteStartElement("input");
                writer.WriteAttributeString("semantic", "BINORMAL");
                writer.WriteAttributeString("source", "#" + geometryname + "_binormal");
                writer.WriteEndElement(); // input
            }
            writer.WriteEndElement(); // vertices
            int texnum = 0;
            foreach (KeyValuePair<string, Texture> texture_kvp in textures)
            {
                string texname = texture_kvp.Key;
                Texture texture = texture_kvp.Value;
                writer.WriteStartElement("triangles");
                writer.WriteAttributeString("count", triangles.Count.ToString());
                if (texture != null)
                {
                    writer.WriteAttributeString("material", texname + "_lnk");
                }
                writer.WriteStartElement("input");
                writer.WriteAttributeString("offset", "0");
                writer.WriteAttributeString("semantic", "VERTEX");
                writer.WriteAttributeString("source", "#" + geometryname + "_vtx");
                writer.WriteEndElement(); // input
                writer.WriteStartElement("input");
                writer.WriteAttributeString("offset", "1");
                writer.WriteAttributeString("semantic", "TEXCOORD");
                writer.WriteAttributeString("source", "#" + geometryname + "_tex");
                writer.WriteAttributeString("set", String.Format("{0}", texnum));
                writer.WriteEndElement(); // input
                writer.WriteStartElement("p");
                foreach (Triangle triangle in triangles.Where(t => t.Texture == texture))
                {
                    writer.WriteWhitespace("\n");
                    foreach (int idx in new int[] { triangle.A.Index, triangle.B.Index, triangle.C.Index })
                    {
                        writer.WriteString(String.Format("{0} {0}  ", idx));
                    }
                }
                writer.WriteWhitespace("\n");
                writer.WriteEndElement(); // p
                writer.WriteEndElement(); // triangles
                texnum++;
            }
            writer.WriteEndElement(); // mesh
            writer.WriteStartElement("extra");
            writer.WriteStartElement("technique");
            writer.WriteAttributeString("profile", "MAYA");
            writer.WriteElementString("double_sided", "1");
            writer.WriteEndElement(); // technique
            writer.WriteEndElement(); // extra
            writer.WriteEndElement(); // geometry

            return vertices;
        }

        public void WriteColladaXml(XmlWriter writer, bool skin, int startframe, int numframes, float framerate)
        {
            writer.WriteStartElement("COLLADA", "http://www.collada.org/2005/11/COLLADASchema");
            writer.WriteAttributeString("version", "1.4.1");
            writer.WriteStartElement("asset");
            writer.WriteStartElement("contributor");
            writer.WriteElementString("author", "IonFx");
            writer.WriteEndElement(); // contributor
            writer.WriteElementString("created", ModTime.ToString("O"));
            writer.WriteElementString("modified", ModTime.ToString("O"));
            writer.WriteStartElement("unit");
            writer.WriteAttributeString("meter", "0.01");
            writer.WriteAttributeString("name", "centimeter");
            writer.WriteEndElement(); // unit
            writer.WriteElementString("up_axis", "Z_UP");
            writer.WriteEndElement(); // asset
            writer.WriteStartElement("library_images");
            
            foreach (KeyValuePair<string, Texture> texture_kvp in Textures)
            {
                string texname = texture_kvp.Key;
                Texture texture = texture_kvp.Value;
                writer.WriteStartElement("image");
                writer.WriteAttributeString("id", texname + "_img");
                writer.WriteAttributeString("name", texname + "_img");
                writer.WriteAttributeString("depth", "1");
                writer.WriteElementString("init_from", String.Join("", this.Name.Where(c => c == '\\').Select(c => "../")) + texture.PNGFilename.Replace('\\', '/'));
                writer.WriteEndElement(); // image
            }
            
            writer.WriteEndElement();
            writer.WriteStartElement("library_effects");
            
            foreach (KeyValuePair<string, Texture> texture_kvp in Textures)
            {
                string texname = texture_kvp.Key;
                Texture texture = texture_kvp.Value;
                writer.WriteStartElement("effect");
                writer.WriteAttributeString("id", texname + "_fx");
                writer.WriteStartElement("profile_COMMON");
                writer.WriteStartElement("newparam");
                writer.WriteAttributeString("sid", texname + "_sfc");
                writer.WriteStartElement("surface");
                writer.WriteAttributeString("type", "2D");
                writer.WriteElementString("init_from", texname + "_img");
                writer.WriteElementString("format", "A8R8G8B8");
                writer.WriteEndElement(); // surface
                writer.WriteEndElement(); // newparam
                writer.WriteStartElement("newparam");
                writer.WriteAttributeString("sid", texname + "_smp");
                writer.WriteStartElement("sampler2D");
                writer.WriteElementString("source", texname + "_sfc");
                writer.WriteElementString("minfilter", "LINEAR_MIPMAP_LINEAR");
                writer.WriteElementString("magfilter", "LINEAR");
                writer.WriteEndElement(); // sampler2D
                writer.WriteEndElement(); // newparam
                writer.WriteStartElement("technique");
                writer.WriteAttributeString("sid", "common");
                writer.WriteStartElement("blinn");
                writer.WriteStartElement("emission");
                writer.WriteElementString("color", "0 0 0 1");
                writer.WriteEndElement(); // emission
                writer.WriteStartElement("ambient");
                writer.WriteElementString("color", "0 0 0 1");
                writer.WriteEndElement(); // ambient
                writer.WriteStartElement("diffuse");
                writer.WriteStartElement("texture");
                writer.WriteAttributeString("texture", texname + "_smp");
                writer.WriteAttributeString("texcoord", "TEXCOORD");
                writer.WriteEndElement(); // texture
                writer.WriteEndElement(); // diffuse
                writer.WriteStartElement("specular");
                writer.WriteElementString("color", "0 0 0 1");
                writer.WriteEndElement(); // specular
                writer.WriteStartElement("shininess");
                writer.WriteElementString("float", "0.2");
                writer.WriteEndElement(); // shininess
                writer.WriteStartElement("reflective");
                writer.WriteElementString("color", "0 0 0 1");
                writer.WriteEndElement(); // reflective
                writer.WriteStartElement("reflectivity");
                writer.WriteElementString("float", "0.2");
                writer.WriteEndElement(); // reflectivity
                writer.WriteStartElement("transparent");
                writer.WriteAttributeString("opaque", "A_ONE");
                writer.WriteStartElement("texture");
                writer.WriteAttributeString("texture", texname + "_smp");
                writer.WriteAttributeString("texcoord", "TEXCOORD");
                writer.WriteEndElement(); // texture
                writer.WriteEndElement(); // transparent
                writer.WriteStartElement("transparency");
                writer.WriteElementString("float", "1.0");
                writer.WriteEndElement(); // transparency
                writer.WriteStartElement("index_of_refraction");
                writer.WriteElementString("float", "1.0");
                writer.WriteEndElement(); // index_of_refraction
                writer.WriteEndElement(); // blinn
                writer.WriteStartElement("extra");
                writer.WriteStartElement("technique");
                writer.WriteAttributeString("profile", "GOOGLEEARTH");
                writer.WriteElementString("double_sided", "1");
                writer.WriteEndElement(); // technique
                writer.WriteStartElement("technique");
                writer.WriteAttributeString("profile", "OKINO");
                writer.WriteElementString("double_sided", "1");
                writer.WriteEndElement(); // technique
                writer.WriteEndElement(); // extra
                writer.WriteEndElement(); // technique
                writer.WriteEndElement(); // profile_COMMON
                writer.WriteStartElement("extra");
                writer.WriteStartElement("technique");
                writer.WriteAttributeString("profile", "MAX3D");
                writer.WriteElementString("double_sided", "1");
                writer.WriteEndElement(); // technique
                writer.WriteEndElement(); // extra
                writer.WriteEndElement(); // effect
            }
            
            writer.WriteEndElement(); // library_effects
            writer.WriteStartElement("library_materials");
            
            foreach (KeyValuePair<string, Texture> texture_kvp in Textures)
            {
                string texname = texture_kvp.Key;
                Texture texture = texture_kvp.Value;
                writer.WriteStartElement("material");
                writer.WriteAttributeString("id", texname + "_mtl");
                writer.WriteStartElement("instance_effect");
                writer.WriteAttributeString("url", "#" + texname + "_fx");
                writer.WriteEndElement(); // instance_effect
                writer.WriteEndElement(); // material
            }
            writer.WriteEndElement(); // library_materials

            Dictionary<string, List<Vertex>> vtxlists = new Dictionary<string,List<Vertex>>();
            Dictionary<string, Dictionary<string, Texture>> texlists = new Dictionary<string, Dictionary<string, Texture>>();
            writer.WriteStartElement("library_geometries");
            
            if (skin)
            {
                Dictionary<string, Texture> texlist = Textures.Where(kvp => ExludeTexturePrefixes.Count(v => kvp.Value.Name.StartsWith(v)) == 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                List<Triangle> triangles = Triangles.Where(t => texlist.Values.Contains(t.Texture)).ToList();
                texlists["model_mesh"] = texlist;
                vtxlists["model_mesh"] = WriteColladaXmlGeometry(writer, "model_mesh", triangles, texlist);
            }
            else
            {
                int modelnum = 0;
                foreach (KeyValuePair<string, Texture> texture_kvp in Textures)
                {
                    string texname = texture_kvp.Key;
                    Texture texture = texture_kvp.Value;
                    string geometryname = String.Format("model{0}_mesh", modelnum);
                    Dictionary<string, Texture> texlist = new Dictionary<string, Texture> { { texname, texture } };
                    List<Triangle> triangles = Triangles.Where(t => t.Texture == texture).ToList();
                    texlists[geometryname] = texlist;
                    vtxlists[geometryname] = WriteColladaXmlGeometry(writer, geometryname, triangles, texlist);
                    modelnum++;
                }
            }
            writer.WriteEndElement(); // library_geometries

            if (Joints.Count != 0)
            {
                List<Joint> joints = Joints.Values.ToList();
                writer.WriteStartElement("library_controllers");
                foreach (KeyValuePair<string, List<Vertex>> vtxlist_kvp in vtxlists)
                {
                    string geometryname = vtxlist_kvp.Key;
                    string skinname = geometryname + "_skin";
                    List<Vertex> vertices = vtxlist_kvp.Value;
                    WriteColladaXmlJointsSkin(writer, geometryname, "model_skel", skinname, vertices, joints);
                }
                writer.WriteEndElement(); // library_controllers

                if (numframes != 0)
                {
                    writer.WriteStartElement("library_animations");
                    WriteColladaXmlAnimation(writer, "model_anim", "model_skel", Animations, startframe, numframes, framerate);
                    writer.WriteEndElement(); // library_animations
                }
            }

            if (!skin)
            {
                writer.WriteStartElement("library_nodes");
                string skeletonname = null;
                bool hasskin = false;
                if (RootJoint != null)
                {
                    skeletonname = "model_skel";
                    hasskin = true;
                    WriteColladaXmlSkeleton(writer, skeletonname, RootJoint, Matrix4.Identity);
                }

                foreach (KeyValuePair<string, Dictionary<string, Texture>> texlist_kvp in texlists)
                {
                    string geometryname = texlist_kvp.Key;
                    Dictionary<string, Texture> texlist = texlist_kvp.Value;
                    WriteColladaXmlGeometryInstance(writer, geometryname + "_node", geometryname, hasskin ? geometryname + "_skin" : null, skeletonname, texlist);
                }

                writer.WriteStartElement("node");
                writer.WriteAttributeString("id", "model_root");
                foreach (KeyValuePair<string, Dictionary<string, Texture>> texlist_kvp in texlists)
                {
                    string geometryname = texlist_kvp.Key;
                    writer.WriteStartElement("instance_node");
                    writer.WriteAttributeString("url", "#" + geometryname + "_node");
                    writer.WriteEndElement(); // instance_node
                }
                writer.WriteEndElement(); // node

                writer.WriteStartElement("node");
                writer.WriteAttributeString("id", "model");
                writer.WriteStartElement("instance_node");
                writer.WriteAttributeString("url", "#model_root");
                writer.WriteEndElement(); // instance_node
                if (skeletonname != null)
                {
                    writer.WriteStartElement("instance_node");
                    writer.WriteAttributeString("url", "#" + skeletonname);
                    writer.WriteEndElement(); // instance_node
                }
                writer.WriteEndElement(); // node
                writer.WriteEndElement(); // library_nodes
            }

            writer.WriteStartElement("library_visual_scenes");
            writer.WriteStartElement("visual_scene");
            writer.WriteAttributeString("id", "visual_scene");
            
            if (skin)
            {
                string skeletonname = null;
                bool hasskin = false;

                if (RootJoint != null)
                {
                    skeletonname = "model_skel";
                    hasskin = true;
                    
                    if (IsAnimated)
                    {
                        foreach (KeyValuePair<string, Joint> joint_kvp in Joints)
                        {
                            joint_kvp.Value.InitialPose = Animations[joint_kvp.Key].Frames[startframe];
                        }
                    }

                    WriteColladaXmlSkeleton(writer, skeletonname, RootJoint, Matrix4.Identity);
                }

                foreach (KeyValuePair<string, Dictionary<string, Texture>> texlist_kvp in texlists)
                {
                    string geometryname = texlist_kvp.Key;
                    Dictionary<string, Texture> texlist = texlist_kvp.Value;
                    WriteColladaXmlGeometryInstance(writer, geometryname + "_node", geometryname, hasskin ? (geometryname + "_skin") : null, skeletonname, texlist);
                }
            }
            else
            {
                writer.WriteStartElement("node");
                writer.WriteAttributeString("id", "model_visual");
                writer.WriteStartElement("instance_node");
                writer.WriteAttributeString("url", "#model");
                writer.WriteEndElement(); // instance_node
                writer.WriteEndElement(); // node
            }

            writer.WriteEndElement(); // visual_scene
            writer.WriteEndElement(); // library_visual_scenes
            writer.WriteStartElement("scene");
            writer.WriteStartElement("instance_visual_scene");
            writer.WriteAttributeString("url", "#visual_scene");
            writer.WriteEndElement(); // instance_visual_scene
            writer.WriteEndElement(); // scene
            writer.WriteEndElement(); // COLLADA
        }

        private void FillModel()
        {
            _Vertices = new List<Vertex>();
            _Triangles = new List<Triangle>();
            _Textures = new Dictionary<string, Texture>();
            _Joints = new Dictionary<string, Joint>();
            _Animations = new Dictionary<string, AnimationSequence>();
            FillModelImpl();
        }

        protected abstract void FillModelImpl();
    }

    public class ImmobileModel : Model
    {
        public ImmobileModel(RS5DirectoryEntry dirent)
            : base(dirent)
        {
        }

        protected override void FillModelImpl()
        {
            RS5Chunk BHDR = Chunk.Chunks["BHDR"];
            RS5Chunk VTXL = Chunk.Chunks["VTXL"];
            RS5Chunk TRIL = Chunk.Chunks["TRIL"];

            Matrix4 roottransform = new Matrix4
            {
                { -1, 0, 0, 0 },
                { 0, 1, 0, 0 },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            };

            for (int texnum = 0; texnum < BHDR.Data.Count / 144; texnum++)
            {
                int texofs = texnum * 144;
                string texsym = String.Format("texture{0}", texnum);
                string texname = BHDR.Data.GetString(texofs, 128);
                int firstvtx = BHDR.Data.GetInt32(texofs + 128);
                int numvtx = BHDR.Data.GetInt32(texofs + 136);
                int firsttri = BHDR.Data.GetInt32(texofs + 132);
                int numtri = BHDR.Data.GetInt32(texofs + 140);
                Texture texture = Texture.GetTexture(texname);

                Textures.Add(texsym, texture);

                int startvtx = Vertices.Count;
                for (int vtxnum = firstvtx; vtxnum < firstvtx + numvtx; vtxnum++)
                {
                    int vtxofs = vtxnum * 36;
                    Vertices.Add(new Vertex
                    {
                        Position = roottransform * new Vector4
                        {
                            X = VTXL.Data.GetSingle(vtxofs + 0),
                            Y = VTXL.Data.GetSingle(vtxofs + 4),
                            Z = VTXL.Data.GetSingle(vtxofs + 8),
                            W = 1.0
                        },
                        TexCoord = new TextureCoordinate
                        {
                            U = VTXL.Data.GetSingle(vtxofs + 24),
                            V = -VTXL.Data.GetSingle(vtxofs + 28),
                            Texture = texture
                        },
                        Normal = roottransform * new Vector4
                        {
                            X = (VTXL.Data[vtxofs + 14] - 0x80) / 127.0,
                            Y = (VTXL.Data[vtxofs + 13] - 0x80) / 127.0,
                            Z = (VTXL.Data[vtxofs + 12] - 0x80) / 127.0,
                            W = 0.0
                        }.Normalize(),
                        Tangent = roottransform * new Vector4
                        {
                            X = (VTXL.Data[vtxofs + 18] - 0x80) / 127.0,
                            Y = (VTXL.Data[vtxofs + 17] - 0x80) / 127.0,
                            Z = (VTXL.Data[vtxofs + 16] - 0x80) / 127.0,
                            W = 0.0
                        }.Normalize(),
                        Binormal = roottransform * new Vector4
                        {
                            X = (VTXL.Data[vtxofs + 22] - 0x80) / 127.0,
                            Y = (VTXL.Data[vtxofs + 21] - 0x80) / 127.0,
                            Z = (VTXL.Data[vtxofs + 20] - 0x80) / 127.0,
                            W = 0.0
                        }.Normalize(),
                        ExtraData = VTXL.Data.GetBytes(vtxofs + 32, 4)
                    });
                }

                for (int trinum = firsttri; trinum < firsttri + numtri - 2; trinum += 3)
                {
                    int triofs = trinum * 4;
                    int a = TRIL.Data.GetInt32(triofs + 0) + startvtx;
                    int b = TRIL.Data.GetInt32(triofs + 4) + startvtx;
                    int c = TRIL.Data.GetInt32(triofs + 8) + startvtx;
                    Triangles.Add(new Triangle
                    {
                        A = Vertices[a],
                        B = Vertices[b],
                        C = Vertices[c],
                        Texture = texture
                    });
                }
            }
        }
    }

    public class AnimatedModel : Model
    {
        public AnimatedModel(RS5DirectoryEntry dirent)
            : base(dirent)
        {
        }

        protected override void FillModelImpl()
        {
            RS5Chunk BLKS = Chunk.Chunks["BLKS"];
            RS5Chunk VTXS = Chunk.Chunks["VTXS"];
            RS5Chunk INDS = Chunk.Chunks["INDS"];
            RS5Chunk JNTS = Chunk.Chunks.ContainsKey("JNTS") ? Chunk.Chunks["JNTS"] : null;
            RS5Chunk FRMS = Chunk.Chunks.ContainsKey("FRMS") ? Chunk.Chunks["FRMS"] : null;

            Matrix4 roottransform = new Matrix4
            {
                { 1, 0, 0, 0 },
                { 0, 0, 1, 0 },
                { 0, -1, 0, 0 },
                { 0, 0, 0, 1 }
            };

            for (int texnum = 0; texnum < BLKS.Data.Count / 144; texnum++)
            {
                int texofs = texnum * 144;
                string texsym = String.Format("texture{0}", texnum);
                string texname = BLKS.Data.GetString(texofs, 128);
                int firstvtx = BLKS.Data.GetInt32(texofs + 128);
                int endvtx = BLKS.Data.GetInt32(texofs + 132);
                int firsttri = BLKS.Data.GetInt32(texofs + 136);
                int endtri = BLKS.Data.GetInt32(texofs + 140);
                Texture texture = Texture.GetTexture(texname);

                Textures.Add(texsym, texture);

                int startvtx = Vertices.Count;
                for (int vtxnum = firstvtx; vtxnum < endvtx; vtxnum++)
                {
                    int vtxofs = vtxnum * 32;
                    Vertices.Add(new Vertex
                    {
                        Position = roottransform * new Vector4
                        {
                            X = VTXS.Data.GetSingle(vtxofs + 0),
                            Y = VTXS.Data.GetSingle(vtxofs + 4),
                            Z = VTXS.Data.GetSingle(vtxofs + 8),
                            W = 1.0
                        },
                        TexCoord = new TextureCoordinate
                        {
                            U = VTXS.Data.GetSingle(vtxofs + 12),
                            V = -VTXS.Data.GetSingle(vtxofs + 16),
                            Texture = texture
                        },
                        Normal = roottransform * new Vector4
                        {
                            X = (VTXS.Data[vtxofs + 22] - 0x80) / 127.0,
                            Y = (VTXS.Data[vtxofs + 21] - 0x80) / 127.0,
                            Z = (VTXS.Data[vtxofs + 20] - 0x80) / 127.0,
                            W = 0.0
                        }.Normalize(),
                        JointInfluence = (new int[] { 0, 1, 2, 3 }).Select(i => new JointInfluence { JointIndex = VTXS.Data[vtxofs + 24 + i], Influence = VTXS.Data[vtxofs + 28 + i] / 255.0F }).Where(j => j.Influence != 0).ToArray()
                    });
                }

                for (int trinum = firsttri; trinum < endtri - 2; trinum += 3)
                {
                    int triofs = trinum * 4;
                    int a = INDS.Data.GetInt32(triofs + 0) - firstvtx + startvtx;
                    int b = INDS.Data.GetInt32(triofs + 4) - firstvtx + startvtx;
                    int c = INDS.Data.GetInt32(triofs + 8) - firstvtx + startvtx;
                    Triangles.Add(new Triangle
                    {
                        A = Vertices[a],
                        B = Vertices[b],
                        C = Vertices[c],
                        Texture = texture
                    });
                }
            }

            if (JNTS != null)
            {
                for (int jntnum = 0; jntnum < JNTS.Data.Count / 196; jntnum++)
                {
                    int jntofs = jntnum * 196;
                    int parent = JNTS.Data.GetInt32(jntofs + 192);
                    string jntsym = String.Format("joint{0}", jntnum);
                    Matrix4 revbindmatrix = new Matrix4
                    {
                        { JNTS.Data.GetSingle(jntofs + 128), JNTS.Data.GetSingle(jntofs + 144), JNTS.Data.GetSingle(jntofs + 160), JNTS.Data.GetSingle(jntofs + 176) },
                        { JNTS.Data.GetSingle(jntofs + 132), JNTS.Data.GetSingle(jntofs + 148), JNTS.Data.GetSingle(jntofs + 164), JNTS.Data.GetSingle(jntofs + 180) },
                        { JNTS.Data.GetSingle(jntofs + 136), JNTS.Data.GetSingle(jntofs + 152), JNTS.Data.GetSingle(jntofs + 168), JNTS.Data.GetSingle(jntofs + 184) },
                        { JNTS.Data.GetSingle(jntofs + 140), JNTS.Data.GetSingle(jntofs + 156), JNTS.Data.GetSingle(jntofs + 172), JNTS.Data.GetSingle(jntofs + 188) }
                    };

                    revbindmatrix /= roottransform;

                    Joint joint = new Joint
                    {
                        Name = JNTS.Data.GetString(jntofs, 128),
                        ReverseBindingMatrix = revbindmatrix,
                        JointNum = jntnum,
                        ParentNum = parent,
                        Symbol = jntsym
                    };
                    Joints.Add(jntsym, joint);
                }

                foreach (Joint joint in Joints.Values)
                {
                    joint.Parent = Joints.Values.Where(j => j.JointNum == joint.ParentNum).FirstOrDefault();
                    joint.Children = Joints.Values.Where(j => j.ParentNum == joint.JointNum).ToArray();
                    if (joint.Parent == null)
                    {
                        joint.InitialPose = 1 / joint.ReverseBindingMatrix;
                    }
                    else
                    {
                        joint.InitialPose = joint.Parent.ReverseBindingMatrix / joint.ReverseBindingMatrix;
                    }
                }

                if (FRMS != null && FRMS.Data != null)
                {
                    Joint[] joints = Joints.Values.ToArray();
                    
                    for (int jntnum = 0; jntnum < joints.Length; jntnum++)
                    {
                        AnimationSequence anim = new AnimationSequence();
                        for (int frameno = 0; frameno < FRMS.Data.Count / (joints.Length * 64); frameno++)
                        {
                            int frameofs = (frameno * Joints.Count + jntnum) * 64;
                            Matrix4 transform = new Matrix4
                            {
                                { FRMS.Data.GetSingle(frameofs +  0), FRMS.Data.GetSingle(frameofs + 16), FRMS.Data.GetSingle(frameofs + 32), FRMS.Data.GetSingle(frameofs + 48) },
                                { FRMS.Data.GetSingle(frameofs +  4), FRMS.Data.GetSingle(frameofs + 20), FRMS.Data.GetSingle(frameofs + 36), FRMS.Data.GetSingle(frameofs + 52) },
                                { FRMS.Data.GetSingle(frameofs +  8), FRMS.Data.GetSingle(frameofs + 24), FRMS.Data.GetSingle(frameofs + 40), FRMS.Data.GetSingle(frameofs + 56) },
                                { FRMS.Data.GetSingle(frameofs + 12), FRMS.Data.GetSingle(frameofs + 28), FRMS.Data.GetSingle(frameofs + 44), FRMS.Data.GetSingle(frameofs + 60) }
                            };
                            
                            if (joints[jntnum].ParentNum == -1)
                            {
                                transform = roottransform * transform;
                            }

                            anim.Frames.Add(frameno, transform);
                        }
                        Animations[joints[jntnum].Symbol] = anim;
                    }
                }
            }
        }
    }
}

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
                return Joints.Values.Where(j => j.Parent == null).FirstOrDefault();
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
        
        public string ColladaFilename
        {
            get
            {
                return ".\\" + Name + ".dae";
            }
        }

        public string ColladaFusedFilename
        {
            get
            {
                return ".\\" + Name + ".fused.dae";
            }
        }

        protected void Save(string filename, bool fused)
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
                    WriteColladaXml(writer, fused);
                }
            }

            File.SetLastWriteTimeUtc(filename, ModTime);
        }

        public void Save()
        {
            Save(ColladaFilename, false);
        }

        public void SaveFused()
        {
            Save(ColladaFusedFilename, true);
        }

        protected float[,] InvertTransformMatrix(float[,] matrix)
        {
            if (matrix[3, 0] != 0 || matrix[3, 1] != 0 || matrix[3, 2] != 0 || matrix[3, 3] != 1)
            {
                throw new InvalidDataException("matrix not a transformation matrix");
            }

            float det = matrix[0, 0] * matrix[1, 1] * matrix[2, 2] +
                        matrix[0, 1] * matrix[1, 2] * matrix[2, 0] +
                        matrix[0, 2] * matrix[1, 0] * matrix[2, 1] -
                        matrix[0, 2] * matrix[1, 1] * matrix[2, 0] -
                        matrix[0, 1] * matrix[1, 0] * matrix[2, 2] -
                        matrix[0, 0] * matrix[1, 2] * matrix[2, 1];

            if (det <= 0.99999 || det >= 1.00001)
            {
                throw new InvalidDataException("matrix scale is not 1.0");
            }

            return new float[4, 4]
            {
                { matrix[0,0], matrix[1,0], matrix[2,0], -(matrix[0,0] * matrix[0,3] + matrix[1,0] * matrix[1,3] + matrix[2,0] * matrix[2,3]) },
                { matrix[0,1], matrix[1,1], matrix[2,1], -(matrix[0,1] * matrix[0,3] + matrix[1,1] * matrix[1,3] + matrix[2,1] * matrix[2,3]) },
                { matrix[0,2], matrix[1,2], matrix[2,2], -(matrix[0,2] * matrix[0,3] + matrix[1,2] * matrix[1,3] + matrix[2,2] * matrix[2,3]) },
                { 0.0F, 0.0F, 0.0F, 1.0F }
            };
        }

        protected float[,] MultiplyMatrix(float[,] matrix1, float[,] matrix2)
        {
            return new float[4, 4]
            {
                {
                    matrix1[0,0] * matrix2[0,0] + matrix1[0,1] * matrix2[1,0] + matrix1[0,2] * matrix2[2,0] + matrix1[0,3] * matrix2[3,0],
                    matrix1[0,0] * matrix2[0,1] + matrix1[0,1] * matrix2[1,1] + matrix1[0,2] * matrix2[2,1] + matrix1[0,3] * matrix2[3,1],
                    matrix1[0,0] * matrix2[0,2] + matrix1[0,1] * matrix2[1,2] + matrix1[0,2] * matrix2[2,2] + matrix1[0,3] * matrix2[3,2],
                    matrix1[0,0] * matrix2[0,3] + matrix1[0,1] * matrix2[1,3] + matrix1[0,2] * matrix2[2,3] + matrix1[0,3] * matrix2[3,3]
                },
                {
                    matrix1[1,0] * matrix2[0,0] + matrix1[1,1] * matrix2[1,0] + matrix1[1,2] * matrix2[2,0] + matrix1[1,3] * matrix2[3,0],
                    matrix1[1,0] * matrix2[0,1] + matrix1[1,1] * matrix2[1,1] + matrix1[1,2] * matrix2[2,1] + matrix1[1,3] * matrix2[3,1],
                    matrix1[1,0] * matrix2[0,2] + matrix1[1,1] * matrix2[1,2] + matrix1[1,2] * matrix2[2,2] + matrix1[1,3] * matrix2[3,2],
                    matrix1[1,0] * matrix2[0,3] + matrix1[1,1] * matrix2[1,3] + matrix1[1,2] * matrix2[2,3] + matrix1[1,3] * matrix2[3,3]
                },
                {
                    matrix1[2,0] * matrix2[0,0] + matrix1[2,1] * matrix2[1,0] + matrix1[2,2] * matrix2[2,0] + matrix1[2,3] * matrix2[3,0],
                    matrix1[2,0] * matrix2[0,1] + matrix1[2,1] * matrix2[1,1] + matrix1[2,2] * matrix2[2,1] + matrix1[2,3] * matrix2[3,1],
                    matrix1[2,0] * matrix2[0,2] + matrix1[2,1] * matrix2[1,2] + matrix1[2,2] * matrix2[2,2] + matrix1[2,3] * matrix2[3,2],
                    matrix1[2,0] * matrix2[0,3] + matrix1[2,1] * matrix2[1,3] + matrix1[2,2] * matrix2[2,3] + matrix1[2,3] * matrix2[3,3]
                },
                {
                    matrix1[3,0] * matrix2[0,0] + matrix1[3,1] * matrix2[1,0] + matrix1[3,2] * matrix2[2,0] + matrix1[3,3] * matrix2[3,0],
                    matrix1[3,0] * matrix2[0,1] + matrix1[3,1] * matrix2[1,1] + matrix1[3,2] * matrix2[2,1] + matrix1[3,3] * matrix2[3,1],
                    matrix1[3,0] * matrix2[0,2] + matrix1[3,1] * matrix2[1,2] + matrix1[3,2] * matrix2[2,2] + matrix1[3,3] * matrix2[3,2],
                    matrix1[3,0] * matrix2[0,3] + matrix1[3,1] * matrix2[1,3] + matrix1[3,2] * matrix2[2,3] + matrix1[3,3] * matrix2[3,3]
                }
            };
        }

        protected float[] TransformVector(float[,] matrix, float[] vector)
        {
            return new float[4]
            { 
                matrix[0,0] * vector[0] + matrix[0,1] * vector[1] + matrix[0,2] * vector[2] + matrix[0,3] * vector[3],
                matrix[1,0] * vector[0] + matrix[1,1] * vector[1] + matrix[1,2] * vector[2] + matrix[1,3] * vector[3],
                matrix[2,0] * vector[0] + matrix[2,1] * vector[1] + matrix[2,2] * vector[2] + matrix[2,3] * vector[3],
                matrix[3,0] * vector[0] + matrix[3,1] * vector[1] + matrix[3,2] * vector[2] + matrix[3,3] * vector[3]
            };
        }

        protected void WriteColladaXmlAnimation(XmlWriter writer, string animationname, string skeletonname, Dictionary<string, AnimationSequence> sequences)
        {
            writer.WriteStartElement("animation");
            foreach (KeyValuePair<string, AnimationSequence> sequence_kvp in sequences)
            {
                string seqname = sequence_kvp.Key;
                string seqid = animationname + "-" + seqname;
                AnimationSequence sequence = sequence_kvp.Value;
                writer.WriteStartElement("animation");
                writer.WriteAttributeString("id", seqid);
                writer.WriteAttributeString("name", seqname);
                writer.WriteStartElement("source");
                writer.WriteAttributeString("id", seqid + "-time");
                writer.WriteStartElement("float_array");
                writer.WriteAttributeString("id", seqid + "-time-array");
                writer.WriteAttributeString("count", sequence.Frames.Count.ToString());
                for (int i = 0; i < sequence.Frames.Count; i++)
                {
                    writer.WriteString(String.Format("{0:F3} ", i / sequence.FrameRate + 1.0));
                }
                writer.WriteEndElement(); // float_array
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString("source", "#" + seqid + "-time-array");
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
                writer.WriteAttributeString("id", seqid + "-out-xfrm");
                writer.WriteStartElement("float_array");
                writer.WriteAttributeString("id", seqid + "-out-xfrm-array");
                writer.WriteAttributeString("count", (sequence.Frames.Count * 16).ToString());
                for (int n = 0; n < sequence.Frames.Count; n++)
                {
                    writer.WriteWhitespace("\n");
                    for (int j = 0; j < 4; j++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            writer.WriteString(String.Format("{0,8:F5} ", sequence.Frames[n][j, i]));
                        }
                    }
                }
                writer.WriteWhitespace("\n");
                writer.WriteEndElement(); // float_array
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString("source", "#" + seqid + "-out-xfrm-array");
                writer.WriteAttributeString("count", sequence.Frames.Count.ToString());
                writer.WriteAttributeString("stride", "16");
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "MATRIX");
                writer.WriteAttributeString("type", "float4x4");
                writer.WriteEndElement(); // param
                writer.WriteEndElement(); // accessor
                writer.WriteEndElement(); // technique_common
                writer.WriteEndElement(); // source
                writer.WriteStartElement("source");
                writer.WriteAttributeString("id", seqid + "-interp");
                writer.WriteStartElement("Name_array");
                writer.WriteAttributeString("id", seqid + "-interp-array");
                writer.WriteAttributeString("count", sequence.Frames.Count.ToString());
                for (int i = 0; i < sequence.Frames.Count; i++)
                {
                    writer.WriteString("LINEAR ");
                }
                writer.WriteEndElement(); // Name_array
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString("source", "#" + seqid + "-interp-array");
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
                writer.WriteAttributeString("id", seqid + "-xfrm");
                writer.WriteStartElement("input");
                writer.WriteAttributeString("semantic", "INPUT");
                writer.WriteAttributeString("source", "#" + seqid + "-time");
                writer.WriteEndElement(); // input
                writer.WriteStartElement("input");
                writer.WriteAttributeString("semantic", "OUTPUT");
                writer.WriteAttributeString("source", "#" + seqid + "-out-xfrm");
                writer.WriteEndElement(); // input
                writer.WriteStartElement("input");
                writer.WriteAttributeString("semantic", "INTERPOLATION");
                writer.WriteAttributeString("source", "#" + seqid + "-interp");
                writer.WriteEndElement(); // input
                writer.WriteEndElement(); // sampler
                writer.WriteStartElement("channel");
                writer.WriteAttributeString("source", "#" + seqid + "-xfrm");
                writer.WriteAttributeString("target", skeletonname + "-" + seqname + "/transform.MATRIX");
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
            writer.WriteAttributeString("id", skinname + "-jnt");
            writer.WriteStartElement("IDREF_array");
            writer.WriteAttributeString("id", skinname + "-jnt-array");
            writer.WriteAttributeString("count", joints.Count.ToString());
            foreach (Joint joint in joints)
            {
                writer.WriteString(skeletonname + "-" + joint.Name + " ");
            }
            writer.WriteEndElement(); // Name_array
            writer.WriteStartElement("technique_common");
            writer.WriteStartElement("accessor");
            writer.WriteAttributeString("source", "#" + skinname + "-jnt-array");
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
            writer.WriteAttributeString("id", skinname + "-bnd");
            writer.WriteStartElement("float_array");
            writer.WriteAttributeString("id", skinname + "-bnd-array");
            writer.WriteAttributeString("count", (joints.Count * 16).ToString());
            foreach (Joint joint in joints)
            {
                writer.WriteWhitespace("\n");
                for (int j = 0; j < 4; j++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        writer.WriteString(String.Format("{0,8:F5} ", joint.Matrix[j, i]));
                    }
                }
            }
            writer.WriteWhitespace("\n");
            writer.WriteEndElement(); // float_array
            writer.WriteStartElement("technique_common");
            writer.WriteStartElement("accessor");
            writer.WriteAttributeString("source", "#" + skinname + "-bnd-array");
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
            writer.WriteAttributeString("id", skinname + "-wgt");
            writer.WriteStartElement("float_array");
            writer.WriteAttributeString("id", skinname + "-wgt-array");
            writer.WriteAttributeString("count", "256");
            for (int i = 0; i < 256; i++)
            {
                writer.WriteString(String.Format("{0,6:F4} ", i / 255.0));
            }
            writer.WriteEndElement(); //float_array
            writer.WriteStartElement("technique_common");
            writer.WriteStartElement("accessor");
            writer.WriteAttributeString("source", "#" + skinname + "-wgt-array");
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
            writer.WriteAttributeString("source", "#" + skinname + "-jnt");
            writer.WriteEndElement(); // input
            writer.WriteStartElement("input");
            writer.WriteAttributeString("semantic", "INV_BIND_MATRIX");
            writer.WriteAttributeString("source", "#" + skinname + "-bnd");
            writer.WriteEndElement(); // input
            writer.WriteEndElement(); // joints
            writer.WriteStartElement("vertex_weights");
            writer.WriteAttributeString("count", vertices.SelectMany(v => v.JointInfluence).Count().ToString());
            writer.WriteStartElement("input");
            writer.WriteAttributeString("offset", "0");
            writer.WriteAttributeString("semantic", "JOINT");
            writer.WriteAttributeString("source", "#" + skinname + "-jnt");
            writer.WriteEndElement(); // input
            writer.WriteStartElement("input");
            writer.WriteAttributeString("offset", "1");
            writer.WriteAttributeString("semantic", "WEIGHT");
            writer.WriteAttributeString("source", "#" + skinname + "-wgt");
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

        protected void WriteColladaXmlSkeleton(XmlWriter writer, string skeletonname, Joint joint, float[,] parentmatrix)
        {
            if (parentmatrix == null)
            {
                parentmatrix = new float[4,4]
                {
                    { 1.0F, 0.0F, 0.0F, 0.0F },
                    { 0.0F, 1.0F, 0.0F, 0.0F },
                    { 0.0F, 0.0F, 1.0F, 0.0F },
                    { 0.0F, 0.0F, 0.0F, 1.0F }
                };
            }

            if (joint.InitialPose == null)
            {
                if (parentmatrix[3, 0] != 0 || parentmatrix[3, 1] != 0 || parentmatrix[3, 2] != 0 || parentmatrix[3, 3] != 1)
                {
                    throw new InvalidDataException("parent matrix not a transformation matrix");
                }

                float[,] invmatrix = InvertTransformMatrix(joint.Matrix);
                float[,] relmatrix = MultiplyMatrix(parentmatrix, invmatrix);
                joint.InitialPose = relmatrix;
            }

            writer.WriteStartElement("node");
            writer.WriteAttributeString("id", skeletonname + "-" + joint.Name);
            writer.WriteAttributeString("name", joint.Name);
            writer.WriteAttributeString("sid", joint.Name);
            writer.WriteAttributeString("type", "JOINT");
            writer.WriteStartElement("matrix");
            writer.WriteAttributeString("sid", "transform");
            for (int j = 0; j < 4; j++)
            {
                writer.WriteWhitespace("\n");
                for (int i = 0; i < 4; i++)
                {
                    writer.WriteString(String.Format("{0,8:F5} ", joint.InitialPose[j,i]));
                }
            }
            writer.WriteWhitespace("\n");
            writer.WriteEndElement(); // matrix
            foreach (Joint childjoint in joint.Children)
            {
                WriteColladaXmlSkeleton(writer, skeletonname, childjoint, joint.Matrix);
            }
            writer.WriteEndElement(); // node
        }

        protected void WriteColladaXmlGeometryInstance(XmlWriter writer, string modelname, string geometryname, string skinname, string skeletonname, Dictionary<string, Texture> textures)
        {
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
                writer.WriteStartElement("bind_material");
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("instance_material");
                writer.WriteAttributeString("symbol", String.Format("{0}-lnk", texname));
                writer.WriteAttributeString("target", String.Format("#{0}-mtl", texname));
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
            writer.WriteAttributeString("id", String.Format("{0}-pos", geometryname));
            writer.WriteStartElement("float_array");
            writer.WriteAttributeString("id", String.Format("{0}-pos-array", geometryname));

            writer.WriteAttributeString("count", (vertices.Count * 3).ToString());
            foreach (Vertex vertex in vertices)
            {
                writer.WriteWhitespace("\n");
                writer.WriteString(String.Format("{0,12:F6} {1,12:F6} {2,12:F6} ", vertex.Position.X, vertex.Position.Y, vertex.Position.Z));
                if (vertex.ExtraData != null)
                {
                    writer.WriteComment(String.Join(" ", vertex.ExtraData.Select(b => String.Format("{0:X2}", b))));
                }
            }
            writer.WriteWhitespace("\n");
            writer.WriteEndElement(); // float_array
            writer.WriteStartElement("technique_common");
            writer.WriteStartElement("accessor");
            writer.WriteAttributeString("count", vertices.Count.ToString());
            writer.WriteAttributeString("offset", "0");
            writer.WriteAttributeString("source", String.Format("#{0}-pos-array", geometryname));
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
            writer.WriteAttributeString("id", String.Format("{0}-tex", geometryname));
            writer.WriteStartElement("float_array");
            writer.WriteAttributeString("id", String.Format("{0}-tex-array", geometryname));
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
            writer.WriteAttributeString("source", String.Format("#{0}-tex-array", geometryname));
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
            writer.WriteStartElement("source");
            writer.WriteAttributeString("id", String.Format("{0}-rgb", geometryname));
            writer.WriteStartElement("float_array");
            writer.WriteAttributeString("id", String.Format("{0}-rgb-array", geometryname));
            writer.WriteAttributeString("count", (vertices.Count * 3).ToString());
            foreach (Vertex vertex in vertices)
            {
                writer.WriteWhitespace("\n");
                writer.WriteString(String.Format("{0:F3} {1:F3} {2:F3}", vertex.Diffuse.R / 255.0, vertex.Diffuse.G / 255.0, vertex.Diffuse.B / 255.0));
            }
            writer.WriteWhitespace("\n");
            writer.WriteEndElement(); // float_array
            writer.WriteStartElement("technique_common");
            writer.WriteStartElement("accessor");
            writer.WriteAttributeString("count", vertices.Count.ToString());
            writer.WriteAttributeString("offset", "0");
            writer.WriteAttributeString("source", String.Format("#{0}-rgb-array", geometryname));
            writer.WriteAttributeString("stride", "3");
            writer.WriteStartElement("param");
            writer.WriteAttributeString("name", "R");
            writer.WriteAttributeString("type", "float");
            writer.WriteEndElement(); // param
            writer.WriteStartElement("param");
            writer.WriteAttributeString("name", "G");
            writer.WriteAttributeString("type", "float");
            writer.WriteEndElement(); // param
            writer.WriteStartElement("param");
            writer.WriteAttributeString("name", "B");
            writer.WriteAttributeString("type", "float");
            writer.WriteEndElement(); // param
            writer.WriteEndElement(); // accessor
            writer.WriteEndElement(); // technique_common
            writer.WriteEndElement(); // source
            writer.WriteStartElement("vertices");
            writer.WriteAttributeString("id", String.Format("{0}-vtx", geometryname));
            writer.WriteStartElement("input");
            writer.WriteAttributeString("semantic", "POSITION");
            writer.WriteAttributeString("source", String.Format("#{0}-pos", geometryname));
            writer.WriteEndElement(); // input
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
                    writer.WriteAttributeString("material", String.Format("{0}-lnk", texname));
                }
                writer.WriteStartElement("input");
                writer.WriteAttributeString("offset", "0");
                writer.WriteAttributeString("semantic", "VERTEX");
                writer.WriteAttributeString("source", String.Format("#{0}-vtx", geometryname));
                writer.WriteEndElement(); // input
                writer.WriteStartElement("input");
                writer.WriteAttributeString("offset", "1");
                writer.WriteAttributeString("semantic", "TEXCOORD");
                writer.WriteAttributeString("source", String.Format("#{0}-tex", geometryname));
                writer.WriteAttributeString("set", String.Format("{0}", texnum));
                writer.WriteEndElement(); // input
                writer.WriteStartElement("input");
                writer.WriteAttributeString("offset", "2");
                writer.WriteAttributeString("semantic", "COLOR");
                writer.WriteAttributeString("source", String.Format("#{0}-rgb", geometryname));
                writer.WriteEndElement(); // input
                writer.WriteStartElement("p");
                foreach (Triangle triangle in triangles.Where(t => t.Texture == texture))
                {
                    writer.WriteWhitespace("\n");
                    foreach (int idx in new int[] { triangle.A.Index, triangle.B.Index, triangle.C.Index })
                    {
                        writer.WriteString(String.Format("{0} {0} {0}  ", idx));
                    }
                }
                writer.WriteWhitespace("\n");
                writer.WriteEndElement(); // p
                writer.WriteEndElement(); // triangles
                texnum++;
            }
            writer.WriteEndElement(); // mesh
            writer.WriteEndElement(); // geometry

            return vertices;
        }
        
        public void WriteColladaXml(XmlWriter writer, bool fused)
        {
            writer.WriteStartElement("COLLADA", "http://www.collada.org/2005/11/COLLADASchema");
            writer.WriteAttributeString("version", "1.4.1");
            writer.WriteStartElement("asset");
            writer.WriteStartElement("contributor");
            writer.WriteElementString("author", "IonFx");
            writer.WriteEndElement(); // contributor
            writer.WriteElementString("created", ModTime.ToString("O"));
            writer.WriteElementString("modified", ModTime.ToString("O"));
            writer.WriteElementString("up_axis", "Z_UP");
            writer.WriteEndElement(); // asset
            writer.WriteStartElement("library_images");
            foreach (KeyValuePair<string, Texture> texture_kvp in Textures)
            {
                string texname = texture_kvp.Key;
                Texture texture = texture_kvp.Value;
                writer.WriteStartElement("image");
                writer.WriteAttributeString("id", String.Format("{0}-img", texname));
                writer.WriteAttributeString("name", String.Format("{0}-img", texname));
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
                writer.WriteAttributeString("id", String.Format("{0}-fx", texname));
                writer.WriteStartElement("profile_COMMON");
                writer.WriteStartElement("newparam");
                writer.WriteAttributeString("sid", String.Format("{0}-sfc", texname));
                writer.WriteStartElement("surface");
                writer.WriteAttributeString("type", "2D");
                writer.WriteElementString("init_from", String.Format("{0}-img", texname));
                writer.WriteElementString("format", "A8R8G8B8");
                writer.WriteEndElement(); // surface
                writer.WriteEndElement(); // newparam
                writer.WriteStartElement("newparam");
                writer.WriteAttributeString("sid", String.Format("{0}-smp", texname));
                writer.WriteStartElement("sampler2D");
                writer.WriteElementString("source", String.Format("{0}-sfc", texname));
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
                writer.WriteAttributeString("texture", String.Format("{0}-smp", texname));
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
                writer.WriteAttributeString("texture", String.Format("{0}-smp", texname));
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
                writer.WriteEndElement(); // technique
                writer.WriteEndElement(); // profile_COMMON
                writer.WriteEndElement(); // effect
            }
            writer.WriteEndElement(); // library_effects
            writer.WriteStartElement("library_materials");
            foreach (KeyValuePair<string, Texture> texture_kvp in Textures)
            {
                string texname = texture_kvp.Key;
                Texture texture = texture_kvp.Value;
                writer.WriteStartElement("material");
                writer.WriteAttributeString("id", String.Format("{0}-mtl", texname));
                writer.WriteStartElement("instance_effect");
                writer.WriteAttributeString("url", String.Format("#{0}-fx", texname));
                writer.WriteEndElement(); // instance_effect
                writer.WriteEndElement(); // material
            }
            writer.WriteEndElement(); // library_materials

            if (fused)
            {
                writer.WriteStartElement("library_geometries");
                List<Vertex> vertices = WriteColladaXmlGeometry(writer, "model-mesh", Triangles, Textures);
                writer.WriteEndElement(); // library_geometries
                if (Joints.Count != 0)
                {
                    List<Joint> joints = Joints.Values.ToList();
                    writer.WriteStartElement("library_controllers");
                    WriteColladaXmlJointsSkin(writer, "model-mesh", "model-skel", "model-skin", vertices, joints);
                    writer.WriteEndElement(); // library_controllers
                    if (Animations.Count != 0)
                    {
                        writer.WriteStartElement("library_animations");
                        WriteColladaXmlAnimation(writer, "model-anim", "model-skel", Animations);
                        writer.WriteEndElement(); // library_animations
                    }
                }
            }
            else
            {
                writer.WriteStartElement("library_geometries");
                int modelnum = 0;
                foreach (KeyValuePair<string, Texture> texture_kvp in Textures)
                {
                    string texname = texture_kvp.Key;
                    Texture texture = texture_kvp.Value;
                    WriteColladaXmlGeometry(writer, String.Format("model{0}-mesh", modelnum), Triangles.Where(t => t.Texture == texture).ToList(), new Dictionary<string, Texture> { { texname, texture } });
                    modelnum++;
                }
                writer.WriteEndElement(); // library_geometries
            }

            writer.WriteStartElement("library_visual_scenes");
            writer.WriteStartElement("visual_scene");
            writer.WriteAttributeString("id", "visual-scene");
            if (fused)
            {
                string skeletonname = null;
                string skinname = null;
                if (RootJoint != null)
                {
                    skeletonname = "model-skel";
                    skinname = "model-skin";
                    WriteColladaXmlSkeleton(writer, skeletonname, RootJoint, null);
                }
                WriteColladaXmlGeometryInstance(writer, "model", "model-mesh", skinname, skeletonname, Textures);
            }
            else
            {
                int modelnum = 0;
                foreach (KeyValuePair<string, Texture> texture_kvp in Textures)
                {
                    string texname = texture_kvp.Key;
                    Texture texture = texture_kvp.Value;
                    WriteColladaXmlGeometryInstance(writer, String.Format("model{0}", modelnum), String.Format("model{0}-mesh", modelnum), null, null, new Dictionary<string, Texture> { { texname, texture } });
                    modelnum++;
                }
            }
            writer.WriteEndElement(); // visual_scene
            writer.WriteEndElement(); // library_visual_scenes
            writer.WriteStartElement("scene");
            writer.WriteStartElement("instance_visual_scene");
            writer.WriteAttributeString("url", "#visual-scene");
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

                if (!(texname.ToUpper().StartsWith("TEX\\CX") ||
                      texname.ToUpper().StartsWith("TEX\\LX") ||
                      texname.ToUpper().StartsWith("TEX\\SPECIAL") ||
                      texname.ToUpper().StartsWith("TEX\\CCX")))
                {
                    Textures.Add(texsym, texture);

                    int startvtx = Vertices.Count;
                    for (int vtxnum = firstvtx; vtxnum < firstvtx + numvtx; vtxnum++)
                    {
                        int vtxofs = vtxnum * 36;
                        Vertices.Add(new Vertex
                        {
                            Position = new Point3D
                            {
                                X = -VTXL.Data.GetSingle(vtxofs + 0),
                                Y = VTXL.Data.GetSingle(vtxofs + 4),
                                Z = VTXL.Data.GetSingle(vtxofs + 8),
                            },
                            TexCoord = new TextureCoordinate
                            {
                                U = VTXL.Data.GetSingle(vtxofs + 24),
                                V = -VTXL.Data.GetSingle(vtxofs + 28),
                            },
                            Texture = texture,
                            Diffuse = Color.FromArgb(VTXL.Data.GetInt32(vtxofs + 12)),
                            ExtraData = VTXL.Data.GetBytes(vtxofs + 12, 12)
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

                if (!(texname.ToUpper().StartsWith("TEX\\CX") ||
                      texname.ToUpper().StartsWith("TEX\\LX") ||
                      texname.ToUpper().StartsWith("TEX\\SPECIAL") ||
                      texname.ToUpper().StartsWith("TEX\\CCX")))
                {
                    Textures.Add(texsym, texture);

                    int startvtx = Vertices.Count;
                    for (int vtxnum = firstvtx; vtxnum < endvtx; vtxnum++)
                    {
                        int vtxofs = vtxnum * 32;
                        Vertices.Add(new Vertex
                        {
                            Position = new Point3D
                            {
                                X = VTXS.Data.GetSingle(vtxofs + 0),
                                Y = VTXS.Data.GetSingle(vtxofs + 4),
                                Z = VTXS.Data.GetSingle(vtxofs + 8)
                            },
                            TexCoord = new TextureCoordinate
                            {
                                U = VTXS.Data.GetSingle(vtxofs + 12),
                                V = -VTXS.Data.GetSingle(vtxofs + 16)
                            },
                            Diffuse = Color.FromArgb(VTXS.Data.GetInt32(vtxofs + 20)),
                            Texture = texture,
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
            }

            if (JNTS != null)
            {
                for (int jntnum = 0; jntnum < JNTS.Data.Count / 196; jntnum++)
                {
                    int jntofs = jntnum * 196;
                    int parent = JNTS.Data.GetInt32(jntofs + 192);
                    float[,] revbindmatrix = new float[4,4]
                    {
                        { JNTS.Data.GetSingle(jntofs + 128), JNTS.Data.GetSingle(jntofs + 144), JNTS.Data.GetSingle(jntofs + 160), JNTS.Data.GetSingle(jntofs + 176) },
                        { JNTS.Data.GetSingle(jntofs + 132), JNTS.Data.GetSingle(jntofs + 148), JNTS.Data.GetSingle(jntofs + 164), JNTS.Data.GetSingle(jntofs + 180) },
                        { JNTS.Data.GetSingle(jntofs + 136), JNTS.Data.GetSingle(jntofs + 152), JNTS.Data.GetSingle(jntofs + 168), JNTS.Data.GetSingle(jntofs + 184) },
                        { JNTS.Data.GetSingle(jntofs + 140), JNTS.Data.GetSingle(jntofs + 156), JNTS.Data.GetSingle(jntofs + 172), JNTS.Data.GetSingle(jntofs + 188) }
                    };

                    Joint joint = new Joint
                    {
                        Name = JNTS.Data.GetString(jntofs, 128),
                        Matrix = revbindmatrix,
                        JointNum = jntnum,
                        ParentNum = parent
                    };
                    Joints.Add(joint.Name, joint);
                }

                foreach (Joint joint in Joints.Values)
                {
                    joint.Parent = Joints.Values.Where(j => j.JointNum == joint.ParentNum).FirstOrDefault();
                    joint.Children = Joints.Values.Where(j => j.ParentNum == joint.JointNum).ToArray();
                }

                if (FRMS != null && FRMS.Data != null)
                {
                    Joint[] joints = Joints.Values.ToArray();
                    
                    for (int jntnum = 0; jntnum < joints.Length; jntnum++)
                    {
                        AnimationSequence anim = new AnimationSequence();
                        anim.FrameRate = 10.0F;
                        for (int frameno = 0; frameno < FRMS.Data.Count / (joints.Length * 64); frameno++)
                        {
                            int frameofs = (frameno * Joints.Count + jntnum) * 64;
                            float[,] transform = new float[4, 4]
                            {
                                { FRMS.Data.GetSingle(frameofs +  0), FRMS.Data.GetSingle(frameofs + 16), FRMS.Data.GetSingle(frameofs + 32), FRMS.Data.GetSingle(frameofs + 48) },
                                { FRMS.Data.GetSingle(frameofs +  4), FRMS.Data.GetSingle(frameofs + 20), FRMS.Data.GetSingle(frameofs + 36), FRMS.Data.GetSingle(frameofs + 52) },
                                { FRMS.Data.GetSingle(frameofs +  8), FRMS.Data.GetSingle(frameofs + 24), FRMS.Data.GetSingle(frameofs + 40), FRMS.Data.GetSingle(frameofs + 56) },
                                { FRMS.Data.GetSingle(frameofs + 12), FRMS.Data.GetSingle(frameofs + 28), FRMS.Data.GetSingle(frameofs + 44), FRMS.Data.GetSingle(frameofs + 60) }
                            };
                            if (frameno == 0)
                            {
                                joints[jntnum].InitialPose = transform;
                            }
                            anim.Frames.Add(transform);
                        }
                        Animations[joints[jntnum].Name] = anim;
                    }
                }
            }
        }
    }
}

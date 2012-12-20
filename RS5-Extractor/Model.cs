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

        protected void WriteColladaXmlSkeleton(XmlWriter writer, string nodeid, Joint joint, float[,] parentmatrix)
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
            if (parentmatrix[3,0] != 0 || parentmatrix[3,1] != 0 || parentmatrix[3,2] != 0 || parentmatrix[3,3] != 1)
            {
                throw new InvalidDataException("parent matrix not a transformation matrix");
            }
            
            if (joint.Matrix[3,0] != 0 || joint.Matrix[3,1] != 0 || joint.Matrix[3,2] != 0 || joint.Matrix[3,3] != 1)
            {
                throw new InvalidDataException("joint matrix not a transformation matrix");
            }

            float det = joint.Matrix[0,0] * joint.Matrix[1,1] * joint.Matrix[2,2] +
                        joint.Matrix[0,1] * joint.Matrix[1,2] * joint.Matrix[2,0] +
                        joint.Matrix[0,2] * joint.Matrix[1,0] * joint.Matrix[2,1] -
                        joint.Matrix[0,2] * joint.Matrix[1,1] * joint.Matrix[2,0] -
                        joint.Matrix[0,1] * joint.Matrix[1,0] * joint.Matrix[2,2] -
                        joint.Matrix[0,0] * joint.Matrix[1,2] * joint.Matrix[2,1];

            if (det <= 0.99999 || det >= 1.00001)
            {
                throw new InvalidDataException("joint matrix scale is not 1.0");
            }

            float[,] invmatrix = new float[4, 4]
            {
                { joint.Matrix[0,0], joint.Matrix[1,0], joint.Matrix[2,0], -(joint.Matrix[0,0] * joint.Matrix[0,3] + joint.Matrix[1,0] * joint.Matrix[1,3] + joint.Matrix[2,0] * joint.Matrix[2,3]) },
                { joint.Matrix[0,1], joint.Matrix[1,1], joint.Matrix[2,1], -(joint.Matrix[0,1] * joint.Matrix[0,3] + joint.Matrix[1,1] * joint.Matrix[1,3] + joint.Matrix[2,1] * joint.Matrix[2,3]) },
                { joint.Matrix[0,2], joint.Matrix[1,2], joint.Matrix[2,2], -(joint.Matrix[0,2] * joint.Matrix[0,3] + joint.Matrix[1,2] * joint.Matrix[1,3] + joint.Matrix[2,2] * joint.Matrix[2,3]) },
                { 0.0F, 0.0F, 0.0F, 1.0F }
            };

            float[,] relmatrix = new float[4, 4]
            {
                {
                    parentmatrix[0,0] * invmatrix[0,0] + parentmatrix[0,1] * invmatrix[1,0] + parentmatrix[0,2] * invmatrix[2,0],
                    parentmatrix[0,0] * invmatrix[0,1] + parentmatrix[0,1] * invmatrix[1,1] + parentmatrix[0,2] * invmatrix[2,1],
                    parentmatrix[0,0] * invmatrix[0,2] + parentmatrix[0,1] * invmatrix[1,2] + parentmatrix[0,2] * invmatrix[2,2],
                    parentmatrix[0,0] * invmatrix[0,3] + parentmatrix[0,1] * invmatrix[1,3] + parentmatrix[0,2] * invmatrix[2,3] + parentmatrix[0,3]
                },
                {
                    parentmatrix[1,0] * invmatrix[0,0] + parentmatrix[1,1] * invmatrix[1,0] + parentmatrix[1,2] * invmatrix[2,0],
                    parentmatrix[1,0] * invmatrix[0,1] + parentmatrix[1,1] * invmatrix[1,1] + parentmatrix[1,2] * invmatrix[2,1],
                    parentmatrix[1,0] * invmatrix[0,2] + parentmatrix[1,1] * invmatrix[1,2] + parentmatrix[1,2] * invmatrix[2,2],
                    parentmatrix[1,0] * invmatrix[0,3] + parentmatrix[1,1] * invmatrix[1,3] + parentmatrix[1,2] * invmatrix[2,3] + parentmatrix[1,3]
                },
                {
                    parentmatrix[2,0] * invmatrix[0,0] + parentmatrix[2,1] * invmatrix[1,0] + parentmatrix[2,2] * invmatrix[2,0],
                    parentmatrix[2,0] * invmatrix[0,1] + parentmatrix[2,1] * invmatrix[1,1] + parentmatrix[2,2] * invmatrix[2,1],
                    parentmatrix[2,0] * invmatrix[0,2] + parentmatrix[2,1] * invmatrix[1,2] + parentmatrix[2,2] * invmatrix[2,2],
                    parentmatrix[2,0] * invmatrix[0,3] + parentmatrix[2,1] * invmatrix[1,3] + parentmatrix[2,2] * invmatrix[2,3] + parentmatrix[2,3]
                },
                { 0.0F, 0.0F, 0.0F, 1.0F }
            };

            writer.WriteStartElement("node");
            if (nodeid != null)
            {
                writer.WriteAttributeString("id", nodeid);
            }
            writer.WriteAttributeString("name", joint.Name);
            writer.WriteAttributeString("sid", joint.Name);
            writer.WriteStartElement("matrix");
            for (int j = 0; j < 4; j++)
            {
                writer.WriteWhitespace("\n");
                for (int i = 0; i < 4; i++)
                {
                    writer.WriteString(String.Format("{0,8:F5} ", relmatrix[j,i]));
                }
            }
            writer.WriteWhitespace("\n");
            writer.WriteEndElement(); // matrix
            foreach (Joint childjoint in joint.Children)
            {
                WriteColladaXmlSkeleton(writer, null, childjoint, joint.Matrix);
            }
            writer.WriteEndElement(); // node
        }

        protected void WriteColladaXmlGeometryInstance(XmlWriter writer, string modelname, Dictionary<string, Texture> textures)
        {
            writer.WriteStartElement("node");
            writer.WriteAttributeString("id", modelname);
            writer.WriteStartElement("rotate");
            writer.WriteAttributeString("sid", "rotateZ");
            writer.WriteString("0 0 1 0");
            writer.WriteEndElement(); // rotate
            writer.WriteStartElement("rotate");
            writer.WriteAttributeString("sid", "rotateY");
            writer.WriteString("0 1 0 0");
            writer.WriteEndElement(); // rotate
            writer.WriteStartElement("rotate");
            writer.WriteAttributeString("sid", "rotateX");
            writer.WriteString("1 0 0 0");
            writer.WriteEndElement(); // rotate
            writer.WriteStartElement("instance_geometry");
            writer.WriteAttributeString("url", String.Format("#{0}-geometry", modelname));
            int texnum = 0;
            foreach (KeyValuePair<string, Texture> texture_kvp in textures)
            {
                string texname = texture_kvp.Key;
                Texture texture = texture_kvp.Value;
                writer.WriteStartElement("bind_material");
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("instance_material");
                writer.WriteAttributeString("symbol", String.Format("{0}-material-link", texname));
                writer.WriteAttributeString("target", String.Format("#{0}-material", texname));
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
            writer.WriteEndElement(); // instance_geometry
            writer.WriteEndElement(); // node
        }
        
        protected void WriteColladaXmlGeometry(XmlWriter writer, string modelname, List<Triangle> triangles, Dictionary<string,Texture> textures)
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
            writer.WriteAttributeString("id", String.Format("{0}-geometry", modelname));
            writer.WriteStartElement("mesh");
            writer.WriteStartElement("source");
            writer.WriteAttributeString("id", String.Format("{0}-vertex-positions", modelname));
            writer.WriteStartElement("float_array");
            writer.WriteAttributeString("id", String.Format("{0}-vertex-positions-array", modelname));

            writer.WriteAttributeString("count", (vertices.Count * 3).ToString());
            foreach (Vertex vertex in vertices)
            {
                writer.WriteWhitespace("\n");
                writer.WriteString(String.Format("{0,12:F6} {1,12:F6} {2,12:F6} ", 
                    vertex.Position.X, vertex.Position.Y, vertex.Position.Z,
                    vertex.TexCoord.U, 1.0 - vertex.TexCoord.V));
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
            writer.WriteAttributeString("source", String.Format("#{0}-vertex-positions-array", modelname));
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
            writer.WriteAttributeString("id", String.Format("{0}-vertex-texcoords", modelname));
            writer.WriteStartElement("float_array");
            writer.WriteAttributeString("id", String.Format("{0}-vertex-texcoords-array", modelname));
            writer.WriteAttributeString("count", (vertices.Count * 2).ToString());
            foreach (Vertex vertex in vertices)
            {
                writer.WriteWhitespace("\n");
                writer.WriteString(String.Format("{0,8:F5} {1,8:F5}", vertex.TexCoord.U, 1.0 - vertex.TexCoord.V));
            }
            writer.WriteWhitespace("\n");
            writer.WriteEndElement(); // float_array
            writer.WriteStartElement("technique_common");
            writer.WriteStartElement("accessor");
            writer.WriteAttributeString("count", vertices.Count.ToString());
            writer.WriteAttributeString("offset", "0");
            writer.WriteAttributeString("source", String.Format("#{0}-vertex-texcoords-array", modelname));
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
            /*
            writer.WriteStartElement("source");
            writer.WriteAttributeString("id", String.Format("{0}-vertex-diffuse", modelname));
            writer.WriteStartElement("float_array");
            writer.WriteAttributeString("id", String.Format("{0}-vertex-diffuse-array", modelname));
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
            writer.WriteAttributeString("source", String.Format("#{0}-vertex-diffuse-array", modelname));
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
             */
            writer.WriteStartElement("vertices");
            writer.WriteAttributeString("id", String.Format("{0}-vertices", modelname));
            writer.WriteStartElement("input");
            writer.WriteAttributeString("semantic", "POSITION");
            writer.WriteAttributeString("source", String.Format("#{0}-vertex-positions", modelname));
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
                    writer.WriteAttributeString("material", String.Format("{0}-material-link", texname));
                }
                writer.WriteStartElement("input");
                writer.WriteAttributeString("offset", "0");
                writer.WriteAttributeString("semantic", "VERTEX");
                writer.WriteAttributeString("source", String.Format("#{0}-vertices", modelname));
                writer.WriteEndElement(); // input
                writer.WriteStartElement("input");
                writer.WriteAttributeString("offset", "1");
                writer.WriteAttributeString("semantic", "TEXCOORD");
                writer.WriteAttributeString("source", String.Format("#{0}-vertex-texcoords", modelname));
                writer.WriteAttributeString("set", String.Format("{0}", texnum));
                writer.WriteEndElement(); // input
                /*
                writer.WriteStartElement("input");
                writer.WriteAttributeString("offset", "2");
                writer.WriteAttributeString("semantic", "COLOR");
                writer.WriteAttributeString("source", String.Format("#{0}-vertex-data", modelname));
                writer.WriteEndElement(); // input
                 */
                writer.WriteStartElement("p");
                foreach (Triangle triangle in triangles.Where(t => t.Texture == texture))
                {
                    writer.WriteWhitespace("\n");
                    foreach (int idx in new int[] { triangle.A.Index, triangle.B.Index, triangle.C.Index })
                    {
                        writer.WriteString(String.Format("{0} {0} ", idx));
                    }
                }
                writer.WriteWhitespace("\n");
                writer.WriteEndElement(); // p
                writer.WriteEndElement(); // triangles
                texnum++;
            }
            writer.WriteEndElement(); // mesh
            writer.WriteEndElement(); // geometry
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
                writer.WriteAttributeString("id", String.Format("{0}-image", texname));
                writer.WriteAttributeString("name", String.Format("{0}-image", texname));
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
                writer.WriteAttributeString("id", String.Format("{0}-effect", texname));
                writer.WriteStartElement("profile_COMMON");
                writer.WriteStartElement("newparam");
                writer.WriteAttributeString("sid", String.Format("{0}-surface", texname));
                writer.WriteStartElement("surface");
                writer.WriteAttributeString("type", "2D");
                writer.WriteElementString("init_from", String.Format("{0}-image", texname));
                writer.WriteElementString("format", "A8R8G8B8");
                writer.WriteEndElement(); // surface
                writer.WriteEndElement(); // newparam
                writer.WriteStartElement("newparam");
                writer.WriteAttributeString("sid", String.Format("{0}-sampler", texname));
                writer.WriteStartElement("sampler2D");
                writer.WriteElementString("source", String.Format("{0}-surface", texname));
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
                writer.WriteAttributeString("texture", String.Format("{0}-sampler", texname));
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
                writer.WriteAttributeString("texture", String.Format("{0}-sampler", texname));
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
                writer.WriteAttributeString("id", String.Format("{0}-material", texname));
                writer.WriteStartElement("instance_effect");
                writer.WriteAttributeString("url", String.Format("#{0}-effect", texname));
                writer.WriteEndElement(); // instance_effect
                writer.WriteEndElement(); // material
            }
            writer.WriteEndElement(); // library_materials
            writer.WriteStartElement("library_geometries");

            if (fused)
            {
                WriteColladaXmlGeometry(writer, "model", Triangles, Textures);
            }
            else
            {
                int modelnum = 0;
                foreach (KeyValuePair<string, Texture> texture_kvp in Textures)
                {
                    string texname = texture_kvp.Key;
                    Texture texture = texture_kvp.Value;
                    WriteColladaXmlGeometry(writer, String.Format("model{0}", modelnum), Triangles.Where(t => t.Texture == texture).ToList(), new Dictionary<string, Texture> { { texname, texture } });
                    modelnum++;
                }
            }

            writer.WriteEndElement(); // library_geometries
            if (fused && Joints.Count != 0)
            {
                List<Joint> joints = Joints.Values.ToList();
                writer.WriteStartElement("library_controllers");
                writer.WriteStartElement("controller");
                writer.WriteAttributeString("id", "model-skin");
                writer.WriteAttributeString("name", "model-skin");
                writer.WriteStartElement("skin");
                writer.WriteAttributeString("source", "#model");
                writer.WriteElementString("bind_shape_matrix", "1 0 0 0  0 1 0 0  0 0 1 0  0 0 0 1");
                writer.WriteStartElement("source");
                writer.WriteAttributeString("id", "model-joint-names");
                writer.WriteStartElement("Name_array");
                writer.WriteAttributeString("id", "model-joint-names-array");
                writer.WriteAttributeString("count", Joints.Count.ToString());
                foreach (Joint joint in joints)
                {
                    writer.WriteString(joint.Name + " ");
                }
                writer.WriteEndElement(); // Name_array
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString("source", "model-joint-names-array");
                writer.WriteAttributeString("count", joints.Count.ToString());
                writer.WriteAttributeString("stride", "1");
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "JOINT");
                writer.WriteAttributeString("type", "Name");
                writer.WriteEndElement(); // param
                writer.WriteEndElement(); // accessor
                writer.WriteEndElement(); // technique_common
                writer.WriteEndElement(); // source
                writer.WriteStartElement("source");
                writer.WriteAttributeString("id", "model-joint-bind_poses");
                writer.WriteStartElement("float_array");
                writer.WriteAttributeString("id", "model-joint-bind_poses-array");
                writer.WriteAttributeString("count", (joints.Count * 16).ToString());
                foreach (Joint joint in joints)
                {
                    writer.WriteWhitespace("\n");
                    for (int j = 0; j < 4; j++)
                    {
                        writer.WriteWhitespace("\n");
                        for (int i = 0; i < 4; i++)
                        {
                            writer.WriteString(String.Format("{0,8:F5} ", joint.Matrix[j,i]));
                        }
                    }
                }
                writer.WriteWhitespace("\n");
                writer.WriteEndElement(); // float_array
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString("source", "model-joint-bind_poses-array");
                writer.WriteAttributeString("count", joints.Count.ToString());
                writer.WriteAttributeString("stride", "16");
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "TRANSFORM");
                writer.WriteAttributeString("type", "float4x4");
                writer.WriteEndElement(); // param
                writer.WriteEndElement(); // accessor
                writer.WriteEndElement(); // technique_common
                writer.WriteEndElement(); // source
                writer.WriteStartElement("joints");
                writer.WriteStartElement("input");
                writer.WriteAttributeString("semantic", "JOINT");
                writer.WriteAttributeString("source", "#model-joint-names");
                writer.WriteEndElement(); // input
                writer.WriteStartElement("input");
                writer.WriteAttributeString("semantic", "INV_BIND_MATRIX");
                writer.WriteAttributeString("source", "#model-joint-bind_poses");
                writer.WriteEndElement(); // input
                writer.WriteEndElement(); // joints
                writer.WriteEndElement(); // skin
                writer.WriteEndElement(); // controller
                writer.WriteEndElement(); // library_controllers
            }
            writer.WriteStartElement("library_visual_scenes");
            writer.WriteStartElement("visual_scene");
            writer.WriteAttributeString("id", "visual-scene");
            if (fused)
            {
                if (RootJoint != null)
                {
                    WriteColladaXmlSkeleton(writer, "skeleton_root", RootJoint, null);
                }
                WriteColladaXmlGeometryInstance(writer, "model", Textures);
            }
            else
            {
                int modelnum = 0;
                foreach (KeyValuePair<string, Texture> texture_kvp in Textures)
                {
                    string texname = texture_kvp.Key;
                    Texture texture = texture_kvp.Value;
                    WriteColladaXmlGeometryInstance(writer, String.Format("model{0}", modelnum), new Dictionary<string, Texture> { { texname, texture } });
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
                                X = VTXL.Data.GetSingle(vtxofs + 0),
                                Y = VTXL.Data.GetSingle(vtxofs + 4),
                                Z = VTXL.Data.GetSingle(vtxofs + 8)
                            },
                            TexCoord = new TextureCoordinate
                            {
                                U = VTXL.Data.GetSingle(vtxofs + 24),
                                V = VTXL.Data.GetSingle(vtxofs + 28),
                            },
                            Texture = texture,
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
            RS5Chunk JNTS = null;
            if (Chunk.Chunks.ContainsKey("JNTS"))
            {
                JNTS = Chunk.Chunks["JNTS"];
            }

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
                                V = VTXS.Data.GetSingle(vtxofs + 16)
                            },
                            Texture = texture,
                            JointInfluence = (new int[] { 0, 1, 2, 3 }).Select(i => new JointInfluence { JointIndex = VTXS.Data[vtxofs + 24 + i], Influence = VTXS.Data[vtxofs + 28 + i] / 255.0F }).Where(j => j.Influence != 0).ToArray(),
                            ExtraData = VTXS.Data.GetBytes(vtxofs + 20, 12)
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
                    Joint joint = new Joint
                    {
                        Name = JNTS.Data.GetString(jntofs, 128),
                        Matrix = new float[4,4]
                        {
                            { JNTS.Data.GetSingle(jntofs + 128), JNTS.Data.GetSingle(jntofs + 144), JNTS.Data.GetSingle(jntofs + 160), JNTS.Data.GetSingle(jntofs + 176) },
                            { JNTS.Data.GetSingle(jntofs + 132), JNTS.Data.GetSingle(jntofs + 148), JNTS.Data.GetSingle(jntofs + 164), JNTS.Data.GetSingle(jntofs + 180) },
                            { JNTS.Data.GetSingle(jntofs + 136), JNTS.Data.GetSingle(jntofs + 152), JNTS.Data.GetSingle(jntofs + 168), JNTS.Data.GetSingle(jntofs + 184) },
                            { JNTS.Data.GetSingle(jntofs + 140), JNTS.Data.GetSingle(jntofs + 156), JNTS.Data.GetSingle(jntofs + 172), JNTS.Data.GetSingle(jntofs + 188) }
                        },
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
            }
        }
    }
}

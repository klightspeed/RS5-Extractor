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

        private List<Texture> _Textures;
        public List<Texture> Textures
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

        public void Save()
        {
            string dir = Path.GetDirectoryName(ColladaFilename);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (Stream stream = File.Open(ColladaFilename, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (XmlWriter writer = XmlWriter.Create(stream, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true }))
                {
                    WriteColladaXml(writer);
                }
            }

            File.SetLastWriteTime(ColladaFilename, ModTime);
        }

        public void WriteColladaXml(XmlWriter writer)
        {
            writer.WriteStartElement("COLLADA", "http://www.collada.org/2005/11/COLLADASchema");
            writer.WriteAttributeString("version", "1.4.1");
            writer.WriteStartElement("asset");
            writer.WriteStartElement("contributor");
            writer.WriteElementString("author", "IonFx");
            writer.WriteEndElement();
            writer.WriteElementString("created", ModTime.ToString("O"));
            writer.WriteElementString("modified", ModTime.ToString("O"));
            writer.WriteElementString("up_axis", "Z_UP");
            writer.WriteEndElement();
            writer.WriteStartElement("library_images");
            for (int i = 0; i < Textures.Count; i++)
            {
                Texture texture = Textures[i];
                writer.WriteStartElement("image");
                writer.WriteAttributeString("id", String.Format("texture{0}-image", i));
                writer.WriteAttributeString("name", String.Format("texture{0}-image", i));
                writer.WriteAttributeString("depth", "1");
                writer.WriteElementString("init_from", String.Join("", this.Name.Where(c => c == '\\').Select(c => "../")) + texture.PNGFilename.Replace('\\', '/'));
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteStartElement("library_effects");
            for (int i = 0; i < Textures.Count; i++)
            {
                Texture texture = Textures[i];
                writer.WriteStartElement("effect");
                writer.WriteAttributeString("id", String.Format("texture{0}-effect", i));
                writer.WriteStartElement("profile_COMMON");
                writer.WriteStartElement("newparam");
                writer.WriteAttributeString("sid", String.Format("texture{0}-surface", i));
                writer.WriteStartElement("surface");
                writer.WriteAttributeString("type", "2D");
                writer.WriteElementString("init_from", String.Format("texture{0}-image", i));
                writer.WriteElementString("format", "A8R8G8B8");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("newparam");
                writer.WriteAttributeString("sid", String.Format("texture{0}-sampler", i));
                writer.WriteStartElement("sampler2D");
                writer.WriteElementString("source", String.Format("texture{0}-surface", i));
                writer.WriteElementString("minfilter", "LINEAR");
                writer.WriteElementString("magfilter", "LINEAR");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("technique");
                writer.WriteAttributeString("sid", String.Format("common", i));
                writer.WriteStartElement("blinn");
                writer.WriteStartElement("diffuse");
                writer.WriteStartElement("texture");
                writer.WriteAttributeString("texture", String.Format("texture{0}-sampler", i));
                writer.WriteAttributeString("texcoord", String.Format("TEX{0}", i));
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("ambient");
                writer.WriteStartElement("texture");
                writer.WriteAttributeString("texture", String.Format("texture{0}-sampler", i));
                writer.WriteAttributeString("texcoord", String.Format("TEX{0}", i));
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("transparent");
                writer.WriteAttributeString("opaque", "A_ONE");
                writer.WriteElementString("color", "1.0 1.0 1.0 1.0");
                writer.WriteEndElement();
                writer.WriteStartElement("transparency");
                writer.WriteElementString("float", "0.0");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteStartElement("library_materials");
            for (int i = 0; i < Textures.Count; i++)
            {
                Texture texture = Textures[i];
                writer.WriteStartElement("material");
                writer.WriteAttributeString("id", String.Format("texture{0}-material", i));
                writer.WriteStartElement("instance_effect");
                writer.WriteAttributeString("url", String.Format("#texture{0}-effect", i));
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteStartElement("library_geometries");
            for (int texnum = 0; texnum < Textures.Count + 1; texnum++)
            {
                Texture texture = texnum >= Textures.Count ? null : Textures[texnum];
                List<Vertex> vertices = new List<Vertex>();
                List<Triangle> triangles = new List<Triangle>();

                int vtxnum = 0;

                foreach (Vertex vertex in Vertices)
                {
                    vertex.Index = -1;
                }

                foreach (Triangle triangle in Triangles)
                {
                    if (triangle.Texture == texture)
                    {
                        triangles.Add(triangle);
                        foreach (Vertex vertex in new Vertex[] { triangle.A, triangle.B, triangle.C })
                        {
                            if (vertex.Index < 0)
                            {
                                vertices.Add(vertex);
                                vertex.Index = vtxnum;
                                vtxnum++;
                            }
                        }
                    }
                }

                writer.WriteStartElement("geometry");
                writer.WriteAttributeString("id", String.Format("model{0}-geometry", texnum));
                writer.WriteStartElement("mesh");
                writer.WriteStartElement("source");
                writer.WriteAttributeString("id", String.Format("model{0}-vertex-positions", texnum));
                writer.WriteStartElement("float_array");
                writer.WriteAttributeString("id", String.Format("model{0}-vertex-positions-array", texnum));

                writer.WriteAttributeString("count", (vertices.Count * 3).ToString());
                for (int i = 0; i < vertices.Count; i++)
                {
                    Vertex vertex = vertices[i];
                    writer.WriteWhitespace("\n");
                    writer.WriteString(String.Format("{0:F6} {1:F6} {2:F6}", vertex.Position.X, vertex.Position.Y, vertex.Position.Z));
                }
                writer.WriteWhitespace("\n");
                writer.WriteEndElement();
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString("count", vertices.Count.ToString());
                writer.WriteAttributeString("offset", "0");
                writer.WriteAttributeString("source", String.Format("#model{0}-vertex-positions-array", texnum));
                writer.WriteAttributeString("stride", "3");
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "X");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement();
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "Y");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement();
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "Z");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("source");
                writer.WriteAttributeString("id", String.Format("model{0}-vertex-texcoords", texnum));
                writer.WriteStartElement("float_array");
                writer.WriteAttributeString("id", String.Format("model{0}-vertex-texcoords-array", texnum));
                writer.WriteAttributeString("count", (vertices.Count * 2).ToString());
                for (int i = 0; i < vertices.Count; i++)
                {
                    Vertex vertex = vertices[i];
                    writer.WriteWhitespace("\n");
                    writer.WriteString(String.Format("{0:F5} {1:F5}", vertex.TexCoord.U, 1.0 - vertex.TexCoord.V));
                }
                writer.WriteWhitespace("\n");
                writer.WriteEndElement();
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString("count", vertices.Count.ToString());
                writer.WriteAttributeString("offset", "0");
                writer.WriteAttributeString("source", String.Format("#model{0}-vertex-texcoords-array", texnum));
                writer.WriteAttributeString("stride", "2");
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "S");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement();
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "T");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("source");
                writer.WriteAttributeString("id", String.Format("model{0}-vertex-diffuse", texnum));
                writer.WriteStartElement("float_array");
                writer.WriteAttributeString("id", String.Format("model{0}-vertex-diffuse-array", texnum));
                writer.WriteAttributeString("count", (vertices.Count * 3).ToString());
                foreach (Vertex vertex in vertices)
                {
                    writer.WriteWhitespace("\n");
                    writer.WriteString(String.Format("{0:F3} {1:F3} {2:F3} {3:F3}", vertex.Diffuse.R / 255.0, vertex.Diffuse.G / 255.0, vertex.Diffuse.B / 255.0, vertex.Diffuse.A / 255.0));
                }
                writer.WriteWhitespace("\n");
                writer.WriteEndElement();
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString("count", vertices.Count.ToString());
                writer.WriteAttributeString("offset", "0");
                writer.WriteAttributeString("source", String.Format("#model{0}-vertex-diffuse-array", texnum));
                writer.WriteAttributeString("stride", "4");
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "R");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement();
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "G");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement();
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "B");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement();
                writer.WriteStartElement("param");
                writer.WriteAttributeString("name", "A");
                writer.WriteAttributeString("type", "float");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("vertices");
                writer.WriteAttributeString("id", String.Format("model{0}-vertices", texnum));
                writer.WriteStartElement("input");
                writer.WriteAttributeString("semantic", "POSITION");
                writer.WriteAttributeString("source", String.Format("#model{0}-vertex-positions", texnum));
                writer.WriteEndElement();
                writer.WriteStartElement("input");
                writer.WriteAttributeString("semantic", "TEXCOORD");
                writer.WriteAttributeString("source", String.Format("#model{0}-vertex-texcoords", texnum));
                writer.WriteAttributeString("set", "0");
                writer.WriteEndElement();
                writer.WriteStartElement("input");
                writer.WriteAttributeString("semantic", "COLOR");
                writer.WriteAttributeString("source", String.Format("#model{0}-vertex-diffuse", texnum));
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteStartElement("triangles");
                writer.WriteAttributeString("count", triangles.Count.ToString());
                if (texture != null)
                {
                    writer.WriteAttributeString("material", String.Format("texture{0}-material-link", texnum));
                }
                writer.WriteStartElement("input");
                writer.WriteAttributeString("offset", "0");
                writer.WriteAttributeString("semantic", "VERTEX");
                writer.WriteAttributeString("source", String.Format("#model{0}-vertices", texnum));
                writer.WriteEndElement();
                writer.WriteStartElement("p");
                foreach (Triangle triangle in triangles)
                {
                    writer.WriteWhitespace("\n");
                    foreach (int idx in new int[] { triangle.A.Index, triangle.B.Index, triangle.C.Index })
                    {
                        writer.WriteString(String.Format("{0} ", idx));
                    }
                }
                writer.WriteWhitespace("\n");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteStartElement("library_visual_scenes");
            writer.WriteStartElement("visual_scene");
            writer.WriteAttributeString("id", "visual-scene");
            for (int texnum = 0; texnum < Textures.Count + 1; texnum++)
            {
                writer.WriteStartElement("node");
                writer.WriteAttributeString("id", String.Format("model{0}", texnum));
                writer.WriteStartElement("rotate");
                writer.WriteAttributeString("sid", "rotateZ");
                writer.WriteString("0 0 1 0");
                writer.WriteEndElement();
                writer.WriteStartElement("rotate");
                writer.WriteAttributeString("sid", "rotateY");
                writer.WriteString("0 1 0 0");
                writer.WriteEndElement();
                writer.WriteStartElement("rotate");
                writer.WriteAttributeString("sid", "rotateX");
                writer.WriteString("1 0 0 0");
                writer.WriteEndElement();
                writer.WriteStartElement("instance_geometry");
                writer.WriteAttributeString("url", String.Format("#model{0}-geometry", texnum));
                if (texnum < Textures.Count)
                {
                    writer.WriteStartElement("bind_material");
                    writer.WriteStartElement("technique_common");
                    writer.WriteStartElement("instance_material");
                    writer.WriteAttributeString("symbol", String.Format("texture{0}-material-link", texnum));
                    writer.WriteAttributeString("target", String.Format("#texture{0}-material", texnum));
                    writer.WriteStartElement("bind_vertex_input");
                    writer.WriteAttributeString("semantic", String.Format("TEX{0}", texnum));
                    writer.WriteAttributeString("input_semantic", "TEXCOORD");
                    writer.WriteAttributeString("input_set", "0");
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteStartElement("scene");
            writer.WriteStartElement("instance_visual_scene");
            writer.WriteAttributeString("url", "#visual-scene");
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private void FillModel()
        {
            _Vertices = new List<Vertex>();
            _Triangles = new List<Triangle>();
            _Textures = new List<Texture>();
            FillModelImpl();
        }

        protected abstract void FillModelImpl();
    }

    public class ImmobileModel : Model
    {
        static string[] ExcludeTextures = new string[]
        {
            "TEX\\CX_PiledRocks",
            "TEX\\CX_Stone",
            "TEX\\CX_Wood",
            "TEX\\LX_Ruins"
        };

        public ImmobileModel(RS5DirectoryEntry dirent)
            : base(dirent)
        {
        }

        protected override void FillModelImpl()
        {
            RS5Chunk BHDR = Chunk.Chunks["BHDR"];
            RS5Chunk VTXL = Chunk.Chunks["VTXL"];
            RS5Chunk TRIL = Chunk.Chunks["TRIL"];
            List<int> TextureVtxStarts = new List<int>();
            List<int> TextureVtxEnds = new List<int>();
            List<int> TextureTriStarts = new List<int>();
            List<int> TextureTriEnds = new List<int>();

            for (int i = 0; i < BHDR.Data.Count; i += 144)
            {
                string texname = BHDR.Data.GetString(i, 128);
                int firstvtx = BHDR.Data.GetInt32(i + 128);
                int numvtx = BHDR.Data.GetInt32(i + 136);
                int firsttri = BHDR.Data.GetInt32(i + 132);
                int numtri = BHDR.Data.GetInt32(i + 140);
                Textures.Add(Texture.GetTexture(texname));
                TextureVtxStarts.Add(firstvtx);
                TextureVtxEnds.Add(firstvtx + numvtx);
                TextureTriStarts.Add(firsttri);
                TextureTriEnds.Add((firsttri + numtri));
            }

            for (int texnum = 0; texnum < Textures.Count; texnum++)
            {
                Texture texture = Textures[texnum];

                if (!ExcludeTextures.Contains(texture.Name))
                {
                    int startvtx = Vertices.Count;
                    for (int i = TextureVtxStarts[texnum]; i < TextureVtxEnds[texnum]; i++)
                    {
                        Vertices.Add(new Vertex
                        {
                            Position = new Point3D
                            {
                                X = VTXL.Data.GetSingle(i * 36 + 0),
                                Y = VTXL.Data.GetSingle(i * 36 + 4),
                                Z = VTXL.Data.GetSingle(i * 36 + 8)
                            },
                            TexCoord = new TextureCoordinate
                            {
                                U = VTXL.Data.GetSingle(i * 36 + 24),
                                V = VTXL.Data.GetSingle(i * 36 + 28),
                            },
                            Diffuse = Color.FromArgb(VTXL.Data.GetInt32(i * 36 + 12)),
                            Ambient = Color.FromArgb(VTXL.Data.GetInt32(i * 36 + 16)),
                            Specular = Color.FromArgb(VTXL.Data.GetInt32(i * 36 + 20)),
                            Texture = texture
                        });
                    }

                    for (int i = TextureTriStarts[texnum]; i < TextureTriEnds[texnum] - 2; i += 3)
                    {
                        int a = TRIL.Data.GetInt32(i * 4 + 0) + startvtx;
                        int b = TRIL.Data.GetInt32(i * 4 + 4) + startvtx;
                        int c = TRIL.Data.GetInt32(i * 4 + 8) + startvtx;
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
            List<int> TextureVtxStarts = new List<int>();
            List<int> TextureVtxEnds = new List<int>();
            List<int> TextureTriStarts = new List<int>();
            List<int> TextureTriEnds = new List<int>();

            for (int i = 0; i < BLKS.Data.Count; i += 144)
            {
                string texname = BLKS.Data.GetString(i, 128);
                int firstvtx = BLKS.Data.GetInt32(i + 128);
                int endvtx = BLKS.Data.GetInt32(i + 132);
                int firsttri = BLKS.Data.GetInt32(i + 136);
                int endtri = BLKS.Data.GetInt32(i + 140);
                Textures.Add(Texture.GetTexture(texname));
                TextureVtxStarts.Add(firstvtx);
                TextureVtxEnds.Add(endvtx);
                TextureTriStarts.Add(firsttri);
                TextureTriEnds.Add(endtri);
            }

            for (int i = 0; i < VTXS.Data.Count; i += 32)
            {
                Vertices.Add(new Vertex
                {
                    Index = i / 32,
                    Position = new Point3D
                    {
                        X = VTXS.Data.GetSingle(i + 0),
                        Y = VTXS.Data.GetSingle(i + 4),
                        Z = VTXS.Data.GetSingle(i + 8)
                    },
                    TexCoord = new TextureCoordinate
                    {
                        U = VTXS.Data.GetSingle(i + 12),
                        V = VTXS.Data.GetSingle(i + 16)
                    },
                    Diffuse = Color.FromArgb(VTXS.Data.GetInt32(i + 20)),
                    Ambient = Color.FromArgb(VTXS.Data.GetInt32(i + 24)),
                    Specular = Color.FromArgb(VTXS.Data.GetInt32(i + 28)),
                    Texture = Textures.Where((t, n) => ((i / 32) >= TextureVtxStarts[n]) && ((i / 32) < TextureVtxEnds[n])).FirstOrDefault()
                });
            }

            for (int texnum = 0; texnum < Textures.Count; texnum++)
            {
                for (int i = TextureTriStarts[texnum]; i < TextureTriEnds[texnum] - 2; i += 3)
                {
                    int a = INDS.Data.GetInt32(i * 4 + 0);
                    int b = INDS.Data.GetInt32(i * 4 + 4);
                    int c = INDS.Data.GetInt32(i * 4 + 8);
                    Triangles.Add(new Triangle
                    {
                        A = Vertices[a],
                        B = Vertices[b],
                        C = Vertices[c],
                        Texture = Textures[texnum]
                    });
                }
            }
        }
    }
}

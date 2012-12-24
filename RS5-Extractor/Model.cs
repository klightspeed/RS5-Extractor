using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Drawing;
using System.IO;
using System.Xml.Linq;

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
        public DateTime CreatTime { get; private set; }

        protected Model(RS5DirectoryEntry dirent)
        {
            this.Chunk = dirent.Data;
            this.Name = dirent.Name;
            this.ModTime = dirent.ModTime;
            this.CreatTime = dirent.ModTime;
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
        
        public string ColladaMultimeshFilename
        {
            get
            {
                return ".\\" + Name + ".multimesh.dae";
            }
        }

        protected string[] ExcludeTexturePrefixes = new string[]
        {
            "TEX\\CX",
            "TEX\\CCX",
            "TEX\\LX",
            "TEX\\SPECIAL"
        };

        protected void Save(string filename, bool skin, bool animate, int startframe, int numframes, float framerate)
        {
            string dir = Path.GetDirectoryName(filename);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            XDocument doc = Collada.Create(this, ExcludeTexturePrefixes, skin, animate, startframe, numframes, framerate);
            if (doc != null)
            {
                doc.Save(filename);
                File.SetLastWriteTimeUtc(filename, ModTime);
            }
        }

        public void SaveMultimesh()
        {
            string filename = ".\\" + Name + ".multimesh.dae";
            Save(filename, false, false, 0, 0, 0);
        }

        public void SaveAnimation(string animname, int startframe, int numframes, float framerate)
        {
            string filename = ".\\" + Name + ".anim." + animname + ".dae";
            Save(filename, true, true, startframe, numframes, framerate);
        }

        public void SaveUnanimated()
        {
            string filename = ".\\" + Name + ".noanim.dae";
            Save(filename, true, false, 0, 0, 0);
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
                        Texture = texture,
                        TextureSymbol = texsym
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
                { -1, 0, 0, 0 },
                { 0, 0, 1, 0 },
                { 0, 1, 0, 0 },
                { 0, 0, 0, 1 }
            };

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
                    Vertex vertex = new Vertex
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
                        }.Normalize()
                    };

                    if (Joints.Count != 0)
                    {
                        List<JointInfluence> influences = new List<JointInfluence>();

                        for (int i = 0; i < 4; i++)
                        {
                            float influence = VTXS.Data[vtxofs + 28 + i] / 255.0F;

                            if (influence != 0)
                            {
                                JointInfluence jntinfluence = new JointInfluence
                                {
                                    JointSymbol = String.Format("joint{0}", VTXS.Data[vtxofs + 24 + i]),
                                    Influence = influence
                                };

                                if (Joints.ContainsKey(jntinfluence.JointSymbol))
                                {
                                    jntinfluence.Joint = Joints[jntinfluence.JointSymbol];
                                    influences.Add(jntinfluence);
                                }
                            }
                        }

                        vertex.JointInfluence = influences.ToArray();
                    }

                    Vertices.Add(vertex);
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
                        Texture = texture,
                        TextureSymbol = texsym
                    });
                }
            }

        }
    }
}

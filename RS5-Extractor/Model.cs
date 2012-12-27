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

        private Mesh _Mesh;
        public Mesh Mesh
        {
            get
            {
                if (_Mesh == null)
                {
                    FillModel();
                }
                return _Mesh;
            }
            private set
            {
                _Mesh = value;
            }
        }

        public IEnumerable<Joint> Joints
        {
            get
            {
                return RootJoint == null ? new Joint[0] : RootJoint.GetSelfAndDescendents();
            }
        }

        public IEnumerable<AnimationSequence> Animations
        {
            get
            {
                return Joints.Select(j => j.Animation).Where(a => a != null);
            }
        }

        private Joint _RootJoint;
        public Joint RootJoint
        {
            get
            {
                if (_RootJoint == null && _Mesh == null)
                {
                    FillModel();
                }
                return _RootJoint;
            }
            protected set
            {
                _RootJoint = value;
            }
        }

        private byte[] _ExtraData;
        public byte[] ExtraData
        {
            get
            {
                if (_ExtraData == null && _Mesh == null)
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

        protected Model(Model model)
        {
            ExtraData = model.ExtraData;
            RootJoint = model.RootJoint.Clone();
            Mesh = model.Mesh.Clone();
            Chunk = model.Chunk;
            Name = model.Name;
            CreatTime = model.CreatTime;
            ModTime = model.ModTime;
            NumAnimationFrames = model.NumAnimationFrames;
        }

        public bool IsAnimated
        {
            get
            {
                return Animations.Count() != 0 && NumAnimationFrames != 0;
            }
        }

        private int? _NumAnimationFrames;
        public int NumAnimationFrames
        {
            get
            {
                if (_NumAnimationFrames == null && _Mesh == null)
                {
                    FillModel();
                }
                return _NumAnimationFrames == null ? 0 : (int)_NumAnimationFrames;
            }
            protected set
            {
                _NumAnimationFrames = value;
            }
        }

        private int? _NumJoints;
        public int NumJoints
        {
            get
            {
                if (_NumJoints == null && _Mesh == null)
                {
                    FillModel();
                }
                return _NumJoints == null ? 0 : (int)_NumJoints;
            }
            protected set
            {
                _NumJoints = value;
            }
        }

        private int? _NumAnimationKeyFrames;
        public int NumAnimationKeyFrames
        {
            get
            {
                if (_NumAnimationKeyFrames == null)
                {
                    
                    HashSet<int> KeyFrames = new HashSet<int>(Animations.Select(anim => anim.Frames.Keys.Select(k => k)).Aggregate((a, v) => a.Union(v)));
                    _NumAnimationKeyFrames = KeyFrames.Count();
                }
                return (int)_NumAnimationKeyFrames;
            }
        }

        public bool IsVisible
        {
            get
            {
                return Mesh.Textures.Values.Where(t => ExcludeTexturePrefixes.Count(v => t.Name.StartsWith(v)) == 0).Count() != 0;
            }
        }

        public bool HasMultipleTextures
        {
            get
            {
                return Mesh.Textures.Count() > 1;
            }
        }

        public bool HasGeometry
        {
            get
            {
                return Mesh.Vertices.Count != 0;
            }
        }

        public bool HasNormals
        {
            get
            {
                return HasGeometry && Mesh.Vertices.First().Normal != Vector4.Zero;
            }
        }

        public bool HasTangents
        {
            get
            {
                return HasGeometry && Mesh.Vertices.First().Tangent != Vector4.Zero;
            }
        }

        public bool HasBinormals
        {
            get
            {
                return HasGeometry && Mesh.Vertices.First().Binormal != Vector4.Zero;
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

        protected void Save(IEnumerable<List<Triangle>> meshes, Joint rootjoint, string filename)
        {
            string dir = Path.GetDirectoryName(filename);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            XDocument doc = new Collada(meshes, rootjoint, Name, CreatTime, ModTime);
            doc.Save(filename);
            File.SetLastWriteTimeUtc(filename, ModTime);
        }

        public void SaveMultimesh()
        {
            string filename = ".\\" + Name + ".multimesh.dae";
            Save(GetSubMeshes(), null, filename);
        }

        public void SaveAnimation(string animname)
        {
            string filename = ".\\" + Name + ".anim." + animname + ".dae";
            Save(GetFiltered(), RootJoint, filename);
        }

        public void SaveUnanimated()
        {
            string filename = ".\\" + Name + ".noanim.dae";
            Save(GetFiltered(), RootJoint == null ? null : RootJoint.WithoutAnimation(), filename);
        }

        protected abstract Model Clone();

        public Model GetAnimatedModel(int startframe, int numframes, float framerate)
        {
            Model model = this.Clone();
            foreach (Joint joint in model.RootJoint.GetSelfAndDescendents())
            {
                joint.Animation = joint.Animation.Trim(startframe, numframes, framerate);
            }
            model.NumAnimationFrames = numframes;
            return model;
        }

        public IEnumerable<List<Triangle>> GetSubMeshes()
        {
            return Mesh.Textures.Select(tex => Mesh.Triangles.Where(tri => tri.TextureSymbol == tex.Key).ToList());
        }

        public IEnumerable<List<Triangle>> GetFiltered()
        {
            return new[] { Mesh.Triangles.Where(t => ExcludeTexturePrefixes.Where(x => t.Texture.Name.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)).Count() == 0).ToList() };
        }

        private void FillModel()
        {
            _Mesh = new Mesh();
            _NumAnimationFrames = 0;
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

        protected ImmobileModel(ImmobileModel model)
            : base(model)
        {
        }

        protected override Model Clone()
        {
            return new ImmobileModel(this);
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

                Mesh.Textures.Add(texsym, texture);

                int startvtx = Mesh.Vertices.Count;
                for (int vtxnum = firstvtx; vtxnum < firstvtx + numvtx; vtxnum++)
                {
                    int vtxofs = vtxnum * 36;
                    Mesh.Vertices.Add(new Vertex
                    {
                        Index = vtxnum,
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
                        },
                        Tangent = roottransform * new Vector4
                        {
                            X = (VTXL.Data[vtxofs + 18] - 0x80) / 127.0,
                            Y = (VTXL.Data[vtxofs + 17] - 0x80) / 127.0,
                            Z = (VTXL.Data[vtxofs + 16] - 0x80) / 127.0,
                            W = 0.0
                        },
                        Binormal = roottransform * new Vector4
                        {
                            X = (VTXL.Data[vtxofs + 22] - 0x80) / 127.0,
                            Y = (VTXL.Data[vtxofs + 21] - 0x80) / 127.0,
                            Z = (VTXL.Data[vtxofs + 20] - 0x80) / 127.0,
                            W = 0.0
                        },
                        ExtraData = VTXL.Data.GetBytes(vtxofs + 32, 4)
                    });
                }

                for (int trinum = firsttri; trinum < firsttri + numtri - 2; trinum += 3)
                {
                    int triofs = trinum * 4;
                    int a = TRIL.Data.GetInt32(triofs + 0) + startvtx;
                    int b = TRIL.Data.GetInt32(triofs + 4) + startvtx;
                    int c = TRIL.Data.GetInt32(triofs + 8) + startvtx;
                    Mesh.Triangles.Add(new Triangle
                    {
                        A = Mesh.Vertices[a],
                        B = Mesh.Vertices[b],
                        C = Mesh.Vertices[c],
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

        protected AnimatedModel(AnimatedModel model)
            : base(model)
        {
        }

        protected override Model Clone()
        {
            return new AnimatedModel(this);
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
                { 0, 0, -1, 0 },
                { 0, 1, 0, 0 },
                { 0, 0, 0, 1 }
            };

            List<Joint> joints = new List<Joint>();

            if (JNTS != null)
            {
                NumJoints = JNTS.Data.Count / 196;
                NumAnimationFrames = (FRMS != null && FRMS.Data != null) ? FRMS.Data.Count / (NumJoints * 64) : 0;
                for (int jntnum = 0; jntnum < NumJoints; jntnum++)
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

                    if (FRMS != null && FRMS.Data != null)
                    {

                        AnimationSequence anim = new AnimationSequence();
                        for (int frameno = 0; frameno < NumAnimationFrames; frameno++)
                        {
                            int frameofs = (frameno * NumJoints + jntnum) * 64;
                            Matrix4 transform = new Matrix4
                        {
                            { FRMS.Data.GetSingle(frameofs +  0), FRMS.Data.GetSingle(frameofs + 16), FRMS.Data.GetSingle(frameofs + 32), FRMS.Data.GetSingle(frameofs + 48) },
                            { FRMS.Data.GetSingle(frameofs +  4), FRMS.Data.GetSingle(frameofs + 20), FRMS.Data.GetSingle(frameofs + 36), FRMS.Data.GetSingle(frameofs + 52) },
                            { FRMS.Data.GetSingle(frameofs +  8), FRMS.Data.GetSingle(frameofs + 24), FRMS.Data.GetSingle(frameofs + 40), FRMS.Data.GetSingle(frameofs + 56) },
                            { FRMS.Data.GetSingle(frameofs + 12), FRMS.Data.GetSingle(frameofs + 28), FRMS.Data.GetSingle(frameofs + 44), FRMS.Data.GetSingle(frameofs + 60) }
                        };

                            if (joint.ParentNum == -1)
                            {
                                transform = roottransform * transform;
                            }

                            anim.Frames.Add(frameno, transform);
                        }
                        joint.Animation = anim.Trim(0, anim.Frames.Count, 24.0);
                    }
                    else
                    {
                        joint.Animation = new AnimationSequence { FrameRate = 24.0, InitialPose = joint.InitialPose, Frames = new SortedDictionary<int, Matrix4>() };
                    }

                    joints.Add(joint);
                }

                foreach (Joint joint in joints)
                {
                    joint.Parent = joints.Where(j => j.JointNum == joint.ParentNum).FirstOrDefault();
                    joint.Children = joints.Where(j => j.ParentNum == joint.JointNum).ToArray();
                    if (joint.Parent == null)
                    {
                        joint.InitialPose = 1 / joint.ReverseBindingMatrix;
                    }
                    else
                    {
                        joint.InitialPose = joint.Parent.ReverseBindingMatrix / joint.ReverseBindingMatrix;
                    }
                }

                RootJoint = joints.Where(j => j.Parent == null).Single();

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

                Mesh.Textures.Add(texsym, texture);

                int startvtx = Mesh.Vertices.Count;
                for (int vtxnum = firstvtx; vtxnum < endvtx; vtxnum++)
                {
                    int vtxofs = vtxnum * 32;
                    Vertex vertex = new Vertex
                    {
                        Index = vtxnum,
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
                        }
                    };

                    List<JointInfluence> influences = new List<JointInfluence>();

                    if (joints.Count != 0)
                    {

                        for (int i = 0; i < 4; i++)
                        {
                            double influence = VTXS.Data[vtxofs + 28 + i] / 255.0;

                            if (influence != 0)
                            {
                                int jointnum = VTXS.Data[vtxofs + 24 + i];
                                JointInfluence jntinfluence = new JointInfluence
                                {
                                    JointSymbol = String.Format("joint{0}", jointnum),
                                    Influence = influence
                                };

                                if (jointnum < joints.Count)
                                {
                                    jntinfluence.Joint = joints[jointnum];
                                    influences.Add(jntinfluence);
                                }
                            }
                        }
                    }

                    vertex.JointInfluence = influences.ToArray();

                    Mesh.Vertices.Add(vertex);
                }

                for (int trinum = firsttri; trinum < endtri - 2; trinum += 3)
                {
                    int triofs = trinum * 4;
                    int a = INDS.Data.GetInt32(triofs + 0) - firstvtx + startvtx;
                    int b = INDS.Data.GetInt32(triofs + 4) - firstvtx + startvtx;
                    int c = INDS.Data.GetInt32(triofs + 8) - firstvtx + startvtx;
                    Mesh.Triangles.Add(new Triangle
                    {
                        A = Mesh.Vertices[a],
                        B = Mesh.Vertices[b],
                        C = Mesh.Vertices[c],
                        Texture = texture,
                        TextureSymbol = texsym
                    });
                }
            }

        }
    }
}

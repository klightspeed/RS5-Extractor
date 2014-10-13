using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Drawing;
using System.IO;
using System.Xml.Linq;

namespace LibRS5
{
    public class Model
    {
        #region ModelBase

        protected abstract class ModelBase
        {
            public readonly IEnumerable<Triangle> Triangles;
            public readonly Joint RootJoint;
            public readonly IEnumerable<XElement> ExtraData;

            protected ModelBase(IEnumerable<Triangle> Triangles, Joint RootJoint, IEnumerable<XElement> ExtraData)
            {
                this.Triangles = Triangles;
                this.RootJoint = RootJoint;
                this.ExtraData = ExtraData == null ? null : ExtraData.Select(d => new XElement(d)).ToArray();
            }

            public static ModelBase Create(RS5DirectoryEntry dirent)
            {
                RS5Chunk chunk = dirent.Data;

                if (dirent.Type == "IMDL")
                {
                    return ImmobileModel.Create(chunk);
                }
                else if (dirent.Type == "AMDL")
                {
                    return AnimatedModel.Create(chunk);
                }
                else
                {
                    throw new ArgumentException("Entry does not represent a model");
                }
            }

            public abstract ModelBase GetAnimated(int startframe, int numframes, double framerate);
        }

        protected class ImmobileModel : ModelBase
        {
            protected static Matrix4 RootTransform = new Matrix4(-1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);

            protected ImmobileModel(IEnumerable<Triangle> triangles, Joint rootjoint, IEnumerable<XElement> extradata)
                : base(triangles, rootjoint, extradata)
            {
            }

            public static ModelBase Create(RS5Chunk chunk)
            {
                Joint rootjoint = GetRootJoint(chunk);
                IEnumerable<Triangle> triangles = GetTriangles(chunk, rootjoint);
                IEnumerable<XElement> extradata = GetExtraData(chunk);
                return new ImmobileModel(triangles, rootjoint, extradata);
            }

            public override ModelBase GetAnimated(int startframe, int numframes, double framerate)
            {
                return this;
            }

            protected static Joint GetRootJoint(RS5Chunk chunk)
            {
                return null;
            }

            protected static XElement GetHEADData(SubStream data, string name)
            {
                int v1 = data.GetInt32(0);
                float v2 = data.GetSingle(4);
                float v3 = data.GetSingle(8);
                float v4 = data.GetSingle(12);
                float v5 = data.GetSingle(16);
                float v6 = data.GetSingle(20);
                float v7 = data.GetSingle(24);
                float v8 = data.GetSingle(28);
                float lodhimindist = data.GetSingle(32);
                float lodhimaxdist = data.GetSingle(36);
                float lodlomindist = data.GetSingle(40);
                float lodlomaxdist = data.GetSingle(44);
                int v13 = data.GetInt32(48);

                return new XElement("IMDL_HEAD",
                    new XElement("v1", v1),
                    new XElement("vf", String.Format("{0,12:F6} {1,12:F6} {2,12:F6} {3,12:F6} {4,12:F6} {5,12:F6} {6,12:F6}", v2, v3, v4, v5, v6, v7, v8)),
                    new XElement("LOD",
                        new XAttribute("level", "high"),
                        new XAttribute("mindist", lodhimindist),
                        new XAttribute("maxdist", lodhimaxdist)
                    ),
                    new XElement("LOD",
                        new XAttribute("level", "low"),
                        new XAttribute("mindist", lodlomindist),
                        new XAttribute("maxdist", lodlomaxdist)
                    ),
                    new XElement("Flags", v13)
                );
            }

            protected static XElement GetTAGLEntry(SubStream data, int index, string name)
            {
                int ofs = index * 84;
                string tagname = data.GetString(ofs + 0, 48);
                Vector4 attachpos = RootTransform * new Vector4(data.GetSingle(ofs + 48), data.GetSingle(ofs + 52), data.GetSingle(ofs + 56), 1.0);
                Vector4 fwdvector = RootTransform * new Vector4(data.GetSingle(ofs + 60), data.GetSingle(ofs + 64), data.GetSingle(ofs + 68), 0.0);
                Vector4 upvector = RootTransform * new Vector4(data.GetSingle(ofs + 72), data.GetSingle(ofs + 76), data.GetSingle(ofs + 80), 0.0);

                return new XElement("IMDL_TAG",
                    new XAttribute("name", tagname),
                    new XElement("Position", String.Format("{0,12:F6} {1,12:F6} {2,12:F6}", attachpos.X, attachpos.Y, attachpos.Z)),
                    new XElement("Forward", String.Format("{0,12:F6} {1,12:F6} {2,12:F6}", fwdvector.X, fwdvector.Y, fwdvector.Z)),
                    new XElement("Up", String.Format("{0,12:F6} {1,12:F6} {2,12:F6}", upvector.X, upvector.Y, upvector.Z))
                );
            }
            
            protected static XElement GetTAGLData(SubStream data, string name)
            {
                int numtags = (int)(data.Length / 84);
                return new XElement("IMDL_TAGL", Enumerable.Range(0, numtags).Select(i => GetTAGLEntry(data, i, name)));
            }

            protected static XElement GetBBHData(SubStream data)
            {
                return new XElement("IMDL_BBH",
                    new XAttribute("count", data.Length / 4),
                    String.Join(" ", Enumerable.Range(0, (int)(data.Length / 4)).Select(i => data.GetInt32(i * 4)))
                );
            }

            protected static XElement GetCOLRData(SubStream data)
            {
                return new XElement("IMDL_COLR",
                    new XAttribute("count", data.Length / 4),
                    String.Join(" ", Enumerable.Range(0, (int)(data.Length / 4)).Select(i => data.GetSingle(i * 4)))
                );
            }

            protected static IEnumerable<XElement> GetExtraData(RS5Chunk chunk)
            {
                RS5Chunk HEAD = chunk.Chunks.ContainsKey("HEAD") ? chunk.Chunks["HEAD"] : null;
                RS5Chunk TAGL = chunk.Chunks.ContainsKey("TAGL") ? chunk.Chunks["TAGL"] : null;
                RS5Chunk BBH = chunk.Chunks.ContainsKey("BBH ") ? chunk.Chunks["BBH "] : null;
                RS5Chunk COLR = chunk.Chunks.ContainsKey("COLR") ? chunk.Chunks["COLR"] : null;
                List<XElement> elements = new List<XElement>();

                if (HEAD != null && HEAD.Data != null)
                {
                    elements.Add(GetHEADData(HEAD.Data, chunk.Name));
                }

                if (TAGL != null && TAGL.Data != null)
                {
                    elements.Add(GetTAGLData(TAGL.Data, chunk.Name));
                }

                if (BBH != null && BBH.Data != null)
                {
                    elements.Add(GetBBHData(BBH.Data));
                }

                if (COLR != null && COLR.Data != null)
                {
                    elements.Add(GetCOLRData(COLR.Data));
                }

                return elements;
            }

            #region Triangles

            protected static Vertex GetVertex(Matrix4 transform, RS5Chunk VTXL, int index, Texture texture)
            {
                int vtxofs = index * 36;
                return new Vertex(
                    transform * new Vector4(VTXL.Data.GetSingle(vtxofs + 0), VTXL.Data.GetSingle(vtxofs + 4), VTXL.Data.GetSingle(vtxofs + 8), 1.0),
                    transform * new Vector4((VTXL.Data.GetByte(vtxofs + 14) - 0x80) / 127.0, (VTXL.Data.GetByte(vtxofs + 13) - 0x80) / 127.0, (VTXL.Data.GetByte(vtxofs + 12) - 0x80) / 127.0, 0.0),
                    transform * new Vector4((VTXL.Data.GetByte(vtxofs + 18) - 0x80) / 127.0, (VTXL.Data.GetByte(vtxofs + 17) - 0x80) / 127.0, (VTXL.Data.GetByte(vtxofs + 16) - 0x80) / 127.0, 0.0),
                    transform * new Vector4((VTXL.Data.GetByte(vtxofs + 22) - 0x80) / 127.0, (VTXL.Data.GetByte(vtxofs + 21) - 0x80) / 127.0, (VTXL.Data.GetByte(vtxofs + 20) - 0x80) / 127.0, 0.0),
                    new TextureCoordinate(texture, VTXL.Data.GetSingle(vtxofs + 24), -VTXL.Data.GetSingle(vtxofs + 28)),
                    null,
                    VTXL.Data.GetBytes(vtxofs + 32, 4)
                );
            }

            protected static Triangle GetTriangle(RS5Chunk TRIL, Vertex[] vertices, int index, Texture texture)
            {
                int triofs = index * 4;
                int a = TRIL.Data.GetInt32(triofs + 0);
                int b = TRIL.Data.GetInt32(triofs + 4);
                int c = TRIL.Data.GetInt32(triofs + 8);
                return new Triangle
                {
                    A = vertices[a],
                    B = vertices[b],
                    C = vertices[c],
                    Texture = texture
                };
            }

            protected static IEnumerable<Triangle> GetTextureTriangles(Matrix4 transform, RS5Chunk BHDR, RS5Chunk VTXL, RS5Chunk TRIL, int index)
            {
                int texofs = index * 144;
                string texname = BHDR.Data.GetString(texofs, 128);
                int firstvtx = BHDR.Data.GetInt32(texofs + 128);
                int numvtx = BHDR.Data.GetInt32(texofs + 136);
                int firsttri = BHDR.Data.GetInt32(texofs + 132);
                int numtri = BHDR.Data.GetInt32(texofs + 140);
                Texture texture = Texture.GetTexture(texname);
                Vertex[] vertices = Enumerable.Range(0, numvtx).Select(i => GetVertex(transform, VTXL, i + firstvtx, texture)).ToArray();
                return Enumerable.Range(0, numtri / 3).Select(i => GetTriangle(TRIL, vertices, i * 3 + firsttri, texture));
            }

            protected static Triangle[] GetTriangles(RS5Chunk chunk, Joint RootJoint)
            {
                RS5Chunk BHDR = chunk.Chunks["BHDR"];
                RS5Chunk VTXL = chunk.Chunks["VTXL"];
                RS5Chunk TRIL = chunk.Chunks["TRIL"];

                int numtextures = (BHDR == null || BHDR.Data == null) ? 0 : (int)(BHDR.Data.Length / 144);

                return Enumerable.Range(0, numtextures).SelectMany(i => GetTextureTriangles(RootTransform, BHDR, VTXL, TRIL, i)).ToArray();
            }

            #endregion
        }

        protected class AnimatedModel : ModelBase
        {
            protected static readonly Matrix4 RootTransform = new Matrix4(1, 0, 0, 0, 0, 0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 1);

            protected AnimatedModel(IEnumerable<Triangle> triangles, Joint rootjoint, IEnumerable<XElement> extradata)
                : base(triangles, rootjoint, extradata)
            {
            }

            public static ModelBase Create(RS5Chunk chunk)
            {
                Joint rootjoint = GetRootJoint(chunk);
                IEnumerable<Triangle> triangles = GetTriangles(chunk, rootjoint);
                IEnumerable<XElement> extradata = GetExtraData(chunk);
                return new AnimatedModel(triangles, rootjoint, extradata);
            }

            public override ModelBase GetAnimated(int startframe, int numframes, double framerate)
            {
                return new AnimatedModel(new List<Triangle>(Triangles), RootJoint.WithTrimmedAnimation(startframe, numframes, framerate), ExtraData);
            }

            #region Joints

            protected class JointData
            {
                public readonly string Name;
                public readonly Matrix4 ReverseBindingMatrix;
                public readonly int Parent;
                public readonly int Index;
                public readonly Matrix4[] AnimationFrames;

                public JointData(RS5Chunk JNTS, RS5Chunk FRMS, int numjoints, int numframes, int index)
                {
                    int offset = index * 196;
                    Index = index;
                    Name = JNTS.Data.GetString(offset, 128);
                    ReverseBindingMatrix = JNTS.Data.GetMatrix4(offset + 128);
                    Parent = JNTS.Data.GetInt32(offset + 192);
                    AnimationFrames = Enumerable.Range(0, numframes).Select(j => FRMS.Data.GetMatrix4((j * numjoints + index) * 64)).ToArray();
                }
            }

            protected class IndexedJoint : Joint
            {
                public readonly int Index;

                public IndexedJoint(int index, string symbol, string name, Matrix4 revbind, Matrix4 initialpose, IEnumerable<Joint> children, AnimationSequence anim)
                    : base(symbol, name, revbind, initialpose, children, anim)
                {
                    Index = index;
                }

                public IndexedJoint(IndexedJoint joint)
                    : base(joint)
                {
                    Index = joint.Index;
                }

                public override Joint Clone()
                {
                    return new IndexedJoint(this);
                }
            }

            protected static AnimationSequence GetAnimation(Matrix4 transform, double framerate, Matrix4 initialpose, Matrix4[] animframes)
            {
                return new AnimationSequence(framerate, initialpose, animframes.Select((f, i) => new AnimationFrame(i, f / transform)), animframes.Length);
            }

            protected static Joint GetJoint(Matrix4 transform, JointData[] jointdata, int index)
            {
                Matrix4 revbind = jointdata[index].ReverseBindingMatrix / transform;
                Matrix4 initialpose = 1 / revbind;
                return new IndexedJoint(
                    index,
                    String.Format("joint{0}", index),
                    jointdata[index].Name,
                    revbind,
                    initialpose,
                    jointdata.Where(j => j.Parent == index).Select(j => GetJoint(Matrix4.Identity, jointdata, j.Index)),
                    GetAnimation(transform, 24.0, initialpose, jointdata[index].AnimationFrames)
                );
            }

            protected static Joint GetRootJoint(RS5Chunk chunk)
            {
                RS5Chunk JNTS = chunk.Chunks.ContainsKey("JNTS") ? chunk.Chunks["JNTS"] : null;
                RS5Chunk FRMS = chunk.Chunks.ContainsKey("FRMS") ? chunk.Chunks["FRMS"] : null;

                if (JNTS != null)
                {
                    int numjoints = (int)(JNTS.Data.Length / 196);
                    int numframes = (FRMS != null && FRMS.Data != null) ? (int)(FRMS.Data.Length / (numjoints * 64)) : 0;

                    JointData[] jointdata = Enumerable.Range(0, numjoints).Select(i => new JointData(JNTS, FRMS, numjoints, numframes, i)).ToArray();

                    return GetJoint(RootTransform, jointdata, jointdata.Where(j => j.Parent == -1).Single().Index);
                }
                else
                {
                    return null;
                }
            }

            #endregion

            #region Triangles

            protected static JointInfluence GetJointInfluence(byte jointnum, byte influence, Joint[] joints)
            {
                return (influence != 0 && jointnum < joints.Length) ? new JointInfluence(joints[jointnum], influence / 255.0) : null;
            }

            protected static Vertex GetVertex(Matrix4 transform, RS5Chunk VTXS, int index, Texture texture, Joint[] joints)
            {
                int vtxofs = index * 32;
                return new Vertex(
                    transform * new Vector4(VTXS.Data.GetSingle(vtxofs + 0), VTXS.Data.GetSingle(vtxofs + 4), VTXS.Data.GetSingle(vtxofs + 8), 1.0),
                    transform * new Vector4((VTXS.Data.GetByte(vtxofs + 22) - 0x80) / 127.0, (VTXS.Data.GetByte(vtxofs + 21) - 0x80) / 127.0, (VTXS.Data.GetByte(vtxofs + 20) - 0x80) / 127.0, 0.0),
                    Vector4.Zero,
                    Vector4.Zero,
                    new TextureCoordinate(texture, VTXS.Data.GetSingle(vtxofs + 12), -VTXS.Data.GetSingle(vtxofs + 16)),
                    Enumerable.Range(0, 4).Select(i => GetJointInfluence(VTXS.Data.GetByte(vtxofs + 24 + i), VTXS.Data.GetByte(vtxofs + 28 + i), joints)).Where(j => j != null),
                    null
                );
            }

            protected static Triangle GetTriangle(RS5Chunk TRIL, Vertex[] vertices, int index, int firstvtx, Texture texture)
            {
                int triofs = index * 4;
                int a = TRIL.Data.GetInt32(triofs + 0) - firstvtx;
                int b = TRIL.Data.GetInt32(triofs + 4) - firstvtx;
                int c = TRIL.Data.GetInt32(triofs + 8) - firstvtx;
                return new Triangle
                {
                    A = vertices[a],
                    B = vertices[b],
                    C = vertices[c],
                    Texture = texture
                };
            }

            protected static IEnumerable<Triangle> GetTextureTriangles(Matrix4 transform, RS5Chunk BLKS, RS5Chunk VTXS, RS5Chunk INDS, int index, Joint[] joints)
            {
                int texofs = index * 144;
                string texname = BLKS.Data.GetString(texofs, 128);
                int firstvtx = BLKS.Data.GetInt32(texofs + 128);
                int endvtx = BLKS.Data.GetInt32(texofs + 132);
                int numvtx = endvtx - firstvtx;
                int firsttri = BLKS.Data.GetInt32(texofs + 136);
                int endtri = BLKS.Data.GetInt32(texofs + 140);
                int numtri = endtri - firsttri;
                Texture texture = Texture.GetTexture(texname);
                Vertex[] vertices = Enumerable.Range(0, numvtx).Select(i => GetVertex(transform, VTXS, i + firstvtx, texture, joints)).ToArray();
                return Enumerable.Range(0, numtri / 3).Select(i => GetTriangle(INDS, vertices, i * 3 + firsttri, firstvtx, texture));
            }

            protected static Triangle[] GetTriangles(RS5Chunk chunk, Joint rootjoint)
            {
                RS5Chunk BLKS = chunk.Chunks["BLKS"];
                RS5Chunk VTXS = chunk.Chunks["VTXS"];
                RS5Chunk INDS = chunk.Chunks["INDS"];

                IndexedJoint[] joints = rootjoint == null ? new IndexedJoint[0] : rootjoint.GetSelfAndDescendents().OfType<IndexedJoint>().OrderBy(j => j.Index).ToArray();

                int numtextures = (BLKS == null || BLKS.Data == null) ? 0 : (int)(BLKS.Data.Length / 144);

                return Enumerable.Range(0, numtextures).SelectMany(i => GetTextureTriangles(RootTransform, BLKS, VTXS, INDS, i, joints)).ToArray();
            }

            #endregion

            protected static IEnumerable<XElement> GetExtraData(RS5Chunk chunk)
            {
                return null;
            }
        }
        
        #endregion

        private readonly static string[] ExcludeTexturePrefixes = new string[] { "TEX\\CX", "TEX\\CCX", "TEX\\LX", "TEX\\SPECIAL" };

        public readonly string Name;
        public readonly DateTime ModTime;
        public readonly DateTime CreatTime;

        #region Lazy-init property backing

        private Func<ModelBase> _ModelDataInitializer;
        private Lazy<ModelBase> _ModelData;
        private Lazy<Vertex[]> _Vertices;
        private Lazy<Texture[]> _Textures;
        private Lazy<Joint[]> _Joints;
        private Lazy<AnimationSequence[]> _Animations;
        private Lazy<bool> _IsVisible;
        private Lazy<bool> _IsAnimated;
        private Lazy<bool> _HasMultipleTextures;
        private Lazy<bool> _HasGeometry;
        private Lazy<bool> _HasNormals;
        private Lazy<bool> _HasTangents;
        private Lazy<bool> _HasBinormals;
        private Lazy<bool> _HasSkeleton;
        private Lazy<int> _NumJoints;
        private Lazy<int> _NumAnimationFrames;
        private Lazy<int> _NumAnimationKeyFrames;
        
        #endregion

        #region Lazy-init properties

        protected ModelBase ModelData { get { return _ModelData.Value; } }
        public IEnumerable<Triangle> Triangles { get { return ModelData.Triangles; } }
        public Joint RootJoint { get { return ModelData.RootJoint; } }
        public IEnumerable<XElement> ExtraData { get { return ModelData.ExtraData; } }
        public IEnumerable<Vertex> Vertices { get { return _Vertices.Value; } }
        public IEnumerable<Texture> Textures { get { return _Textures.Value; } }
        public IEnumerable<Joint> Joints { get { return _Joints.Value; } }
        public IEnumerable<AnimationSequence> Animations { get { return _Animations.Value; } }
        public bool IsVisible { get { return _IsVisible.Value; } }
        public bool IsAnimated { get { return _IsAnimated.Value; } }
        public bool HasMultipleTextures { get { return _HasMultipleTextures.Value; } }
        public bool HasGeometry { get { return _HasGeometry.Value; } }
        public bool HasNormals { get { return _HasNormals.Value; } }
        public bool HasTangents { get { return _HasTangents.Value; } }
        public bool HasBinormals { get { return _HasBinormals.Value; } }
        public bool HasSkeleton { get { return _HasSkeleton.Value; } }
        public int NumJoints { get { return _NumJoints.Value; } }
        public int NumAnimationFrames { get { return _NumAnimationFrames.Value; } }
        public int NumAnimationKeyFrames { get { return _NumAnimationKeyFrames.Value; } }
        
        #endregion

        #region Lazy initialization

        private void InitLazyProps()
        {
            this._ModelData = new Lazy<ModelBase>(() => _ModelDataInitializer());
            this._Vertices = new Lazy<Vertex[]>(() => Triangles.SelectMany(t => t).Union(new Vertex[0]).ToArray());
            this._Textures = new Lazy<Texture[]>(() => Triangles.Select(t => t.Texture).Union(new Texture[0]).ToArray());
            this._Joints = new Lazy<Joint[]>(() => RootJoint == null ? new Joint[0] : RootJoint.GetSelfAndDescendents().ToArray());
            this._Animations = new Lazy<AnimationSequence[]>(() => Joints.Select(j => j.Animation).ToArray());
            this._IsVisible = new Lazy<bool>(() => Textures.Where(t => ExcludeTexturePrefixes.Count(v => t.Name.StartsWith(v)) == 0).Count() != 0);
            this._IsAnimated = new Lazy<bool>(() => Animations.Count() != 0 && NumAnimationKeyFrames != 0);
            this._HasMultipleTextures = new Lazy<bool>(() => Textures.Count() > 1);
            this._HasGeometry = new Lazy<bool>(() => Triangles.Count() != 0);
            this._HasNormals = new Lazy<bool>(() => HasGeometry && Vertices.First().Normal != Vector4.Zero);
            this._HasTangents = new Lazy<bool>(() => HasGeometry && Vertices.First().Tangent != Vector4.Zero);
            this._HasBinormals = new Lazy<bool>(() => HasGeometry && Vertices.First().Binormal != Vector4.Zero);
            this._HasSkeleton = new Lazy<bool>(() => RootJoint != null);
            this._NumJoints = new Lazy<int>(() => Joints.Count());
            this._NumAnimationFrames = new Lazy<int>(() => Animations.Select(a => a.NumFrames).Union(new[] { 0 }).Max());
            this._NumAnimationKeyFrames = new Lazy<int>(() => Animations.Select(anim => anim.Frames.Select(f => f.FrameNum)).Aggregate((a, v) => a.Union(v)).Count());
        }

        #endregion

        #region Constructors

        private Model()
        {
            InitLazyProps();
            _ModelDataInitializer = () => null;
        }

        public Model(RS5DirectoryEntry dirent)
            : this()
        {
            this.Name = dirent.Name;
            this.ModTime = dirent.ModTime;
            this.CreatTime = dirent.ModTime;
            this._ModelDataInitializer = () => ModelBase.Create(dirent);
        }

        protected Model(Model model, Func<ModelBase> modeldata)
            : this()
        {
            this.Name = model.Name;
            this.CreatTime = model.CreatTime;
            this.ModTime = model.ModTime;
            this._ModelDataInitializer = modeldata;
        }
        
        #endregion

        #region Model exporting

        public Collada.Exporter CreateMultimeshExporter(bool overwrite)
        {
            return new Collada.Exporter("." + Path.DirectorySeparatorChar + Name + ".multimesh.dae", () => HasMultipleTextures, () => GetSubMeshes(), () => null, () => ExtraData, CreatTime, ModTime, overwrite);
        }

        public Collada.Exporter<T> CreateMultimeshExporter<T>(bool overwrite, T state)
        {
            return new Collada.Exporter<T>("." + Path.DirectorySeparatorChar + Name + ".multimesh.dae", () => HasMultipleTextures, () => GetSubMeshes(), () => null, () => ExtraData, CreatTime, ModTime, overwrite, state);
        }

        public Collada.Exporter CreateUnanimatedExporter(bool overwrite)
        {
            return new Collada.Exporter("." + Path.DirectorySeparatorChar + Name + ".noanim.dae", () => true, () => GetFiltered(), () => RootJoint == null ? null : RootJoint.WithoutAnimation(), () => ExtraData, CreatTime, ModTime, overwrite);
        }

        public Collada.Exporter<T> CreateUnanimatedExporter<T>(bool overwrite, T state)
        {
            return new Collada.Exporter<T>("." + Path.DirectorySeparatorChar + Name + ".noanim.dae", () => true, () => GetFiltered(), () => RootJoint == null ? null : RootJoint.WithoutAnimation(), () => ExtraData, CreatTime, ModTime, overwrite, state);
        }

        public Collada.Exporter CreateAnimatedExporter(string animname, bool overwrite)
        {
            return new Collada.Exporter("." + Path.DirectorySeparatorChar + Name + ".anim." + animname + ".dae", () => IsAnimated, () => GetFiltered(), () => RootJoint, () => ExtraData, CreatTime, ModTime, overwrite);
        }

        public Collada.Exporter<T> CreateAnimatedExporter<T>(string animname, bool overwrite, T state)
        {
            return new Collada.Exporter<T>("." + Path.DirectorySeparatorChar + Name + ".anim." + animname + ".dae", () => IsAnimated, () => GetFiltered(), () => RootJoint, () => ExtraData, CreatTime, ModTime, overwrite, state);
        }

        public Collada.Exporter CreateTrimmedAnimatedExporter(string animname, int startframe, int numframes, double framerate, bool overwrite)
        {
            Model model = GetAnimatedModel(startframe, numframes, framerate);
            return new Collada.Exporter("." + Path.DirectorySeparatorChar + Name + ".anim." + animname + ".dae", () => model.IsAnimated, () => model.GetFiltered(), () => model.RootJoint, () => ExtraData, model.CreatTime, model.ModTime, overwrite);
        }

        public Collada.Exporter<T> CreateTrimmedAnimatedExporter<T>(string animname, int startframe, int numframes, double framerate, bool overwrite, T state)
        {
            Model model = GetAnimatedModel(startframe, numframes, framerate);
            return new Collada.Exporter<T>("." + Path.DirectorySeparatorChar + Name + ".anim." + animname + ".dae", () => model.IsAnimated, () => model.GetFiltered(), () => model.RootJoint, () => ExtraData, model.CreatTime, model.ModTime, overwrite, state);
        }

        #endregion

        #region Model filtering
        
        public Model GetAnimatedModel(int startframe, int numframes, double framerate)
        {
            return new Model(this, () => ModelData.GetAnimated(startframe, numframes, framerate));
        }

        public IEnumerable<List<Triangle>> GetSubMeshes()
        {
            return Textures.Select(tex => Triangles.Where(tri => tri.Texture.Name == tex.Name).ToList());
        }

        public IEnumerable<List<Triangle>> GetFiltered()
        {
            return new[] { Triangles.Where(t => ExcludeTexturePrefixes.Where(x => t.Texture.Name.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)).Count() == 0).ToList() };
        }
        
        #endregion
    }
}

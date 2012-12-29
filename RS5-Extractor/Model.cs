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
    public class Model
    {
        #region ModelBase

        protected abstract class ModelBase
        {
            public readonly IEnumerable<Triangle> Triangles;
            public readonly Joint RootJoint;
            public readonly byte[] ExtraData;

            protected ModelBase(IEnumerable<Triangle> Triangles, Joint RootJoint, byte[] ExtraData)
            {
                this.Triangles = Triangles;
                this.RootJoint = RootJoint;
                this.ExtraData = ExtraData;
            }

            public static ModelBase Create(RS5DirectoryEntry dirent)
            {
                RS5Chunk chunk = dirent.GetData();

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
            protected ImmobileModel(IEnumerable<Triangle> triangles, Joint rootjoint, byte[] extradata)
                : base(triangles, rootjoint, extradata)
            {
            }

            public static ModelBase Create(RS5Chunk chunk)
            {
                Joint rootjoint = GetRootJoint(chunk);
                IEnumerable<Triangle> triangles = GetTriangles(chunk, rootjoint);
                byte[] extradata = GetExtraData(chunk);
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

            protected static byte[] GetExtraData(RS5Chunk chunk)
            {
                return null;
            }

            #region Triangles

            protected static Vertex GetVertex(Matrix4 transform, RS5Chunk VTXL, int index, Texture texture)
            {
                int vtxofs = index * 36;
                return new Vertex(
                    transform * new Vector4(VTXL.Data.GetSingle(vtxofs + 0), VTXL.Data.GetSingle(vtxofs + 4), VTXL.Data.GetSingle(vtxofs + 8), 1.0),
                    transform * new Vector4((VTXL.Data[vtxofs + 14] - 0x80) / 127.0, (VTXL.Data[vtxofs + 13] - 0x80) / 127.0, (VTXL.Data[vtxofs + 12] - 0x80) / 127.0, 0.0),
                    transform * new Vector4((VTXL.Data[vtxofs + 18] - 0x80) / 127.0, (VTXL.Data[vtxofs + 17] - 0x80) / 127.0, (VTXL.Data[vtxofs + 16] - 0x80) / 127.0, 0.0),
                    transform * new Vector4((VTXL.Data[vtxofs + 22] - 0x80) / 127.0, (VTXL.Data[vtxofs + 21] - 0x80) / 127.0, (VTXL.Data[vtxofs + 20] - 0x80) / 127.0, 0.0),
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

                Matrix4 roottransform = new Matrix4(-1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
                int numtextures = (BHDR == null || BHDR.Data == null) ? 0 : BHDR.Data.Count / 144;

                return Enumerable.Range(0, numtextures).SelectMany(i => GetTextureTriangles(roottransform, BHDR, VTXL, TRIL, i)).ToArray();
            }

            #endregion
        }

        protected class AnimatedModel : ModelBase
        {
            protected static readonly Matrix4 RootTransform = new Matrix4(1, 0, 0, 0, 0, 0, -1, 0, 0, 1, 0, 0, 0, 0, 0, 1);

            protected AnimatedModel(IEnumerable<Triangle> triangles, Joint rootjoint, byte[] extradata)
                : base(triangles, rootjoint, extradata)
            {
            }

            public static ModelBase Create(RS5Chunk chunk)
            {
                Joint rootjoint = GetRootJoint(chunk);
                IEnumerable<Triangle> triangles = GetTriangles(chunk, rootjoint);
                byte[] extradata = GetExtraData(chunk);
                return new AnimatedModel(triangles, rootjoint, extradata);
            }

            public override ModelBase GetAnimated(int startframe, int numframes, double framerate)
            {
                return new AnimatedModel(new List<Triangle>(Triangles), RootJoint.WithTrimmedAnimation(startframe, numframes, framerate), ExtraData.ToArray());
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
                    int numjoints = JNTS.Data.Count / 196;
                    int numframes = (FRMS != null && FRMS.Data != null) ? FRMS.Data.Count / (numjoints * 64) : 0;

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
                    transform * new Vector4((VTXS.Data[vtxofs + 22] - 0x80) / 127.0, (VTXS.Data[vtxofs + 21] - 0x80) / 127.0, (VTXS.Data[vtxofs + 20] - 0x80) / 127.0, 0.0),
                    Vector4.Zero,
                    Vector4.Zero,
                    new TextureCoordinate(texture, VTXS.Data.GetSingle(vtxofs + 12), -VTXS.Data.GetSingle(vtxofs + 16)),
                    Enumerable.Range(0, 4).Select(i => GetJointInfluence(VTXS.Data[vtxofs + 24 + i], VTXS.Data[vtxofs + 28 + i], joints)).Where(j => j != null),
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

                IndexedJoint[] joints = rootjoint.GetSelfAndDescendents().OfType<IndexedJoint>().OrderBy(j => j.Index).ToArray();

                int numtextures = (BLKS == null || BLKS.Data == null) ? 0 : BLKS.Data.Count / 144;

                return Enumerable.Range(0, numtextures).SelectMany(i => GetTextureTriangles(RootTransform, BLKS, VTXS, INDS, i, joints)).ToArray();
            }

            #endregion

            protected static byte[] GetExtraData(RS5Chunk chunk)
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
        public byte[] ExtraData { get { return ModelData.ExtraData; } }
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

        #region Constructors

        private Model()
        {
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

        public Model(RS5DirectoryEntry dirent)
            : this()
        {
            this.Name = dirent.Name;
            this.ModTime = dirent.ModTime;
            this.CreatTime = dirent.ModTime;
            this._ModelData = new Lazy<ModelBase>(() => ModelBase.Create(dirent));
        }

        protected Model(Model model, Func<ModelBase> modeldata)
            : this()
        {
            this.Name = model.Name;
            this.CreatTime = model.CreatTime;
            this.ModTime = model.ModTime;
            this._ModelData = new Lazy<ModelBase>(modeldata);
        }
        
        #endregion

        #region Model exporting

        public Collada.Exporter CreateMultimeshExporter(bool overwrite)
        {
            return new Collada.Exporter("." + Path.DirectorySeparatorChar + Name + ".multimesh.dae", () => HasMultipleTextures, () => GetSubMeshes(), () => null, CreatTime, ModTime, overwrite);
        }

        public Collada.Exporter<T> CreateMultimeshExporter<T>(bool overwrite, T state)
        {
            return new Collada.Exporter<T>("." + Path.DirectorySeparatorChar + Name + ".multimesh.dae", () => HasMultipleTextures, () => GetSubMeshes(), () => null, CreatTime, ModTime, overwrite, state);
        }

        public Collada.Exporter CreateUnanimatedExporter(bool overwrite)
        {
            return new Collada.Exporter("." + Path.DirectorySeparatorChar + Name + ".noanim..dae", () => true, () => GetFiltered(), () => RootJoint == null ? null : RootJoint.WithoutAnimation(), CreatTime, ModTime, overwrite);
        }

        public Collada.Exporter<T> CreateUnanimatedExporter<T>(bool overwrite, T state)
        {
            return new Collada.Exporter<T>("." + Path.DirectorySeparatorChar + Name + ".noanim..dae", () => true, () => GetFiltered(), () => RootJoint == null ? null : RootJoint.WithoutAnimation(), CreatTime, ModTime, overwrite, state);
        }

        public Collada.Exporter CreateAnimatedExporter(string animname, bool overwrite)
        {
            return new Collada.Exporter("." + Path.DirectorySeparatorChar + Name + ".anim." + animname + ".dae", () => IsAnimated, () => GetFiltered(), () => RootJoint, CreatTime, ModTime, overwrite);
        }

        public Collada.Exporter<T> CreateAnimatedExporter<T>(string animname, bool overwrite, T state)
        {
            return new Collada.Exporter<T>("." + Path.DirectorySeparatorChar + Name + ".anim." + animname + ".dae", () => IsAnimated, () => GetFiltered(), () => RootJoint, CreatTime, ModTime, overwrite, state);
        }

        public Collada.Exporter CreateTrimmedAnimatedExporter(string animname, int startframe, int numframes, double framerate, bool overwrite)
        {
            Model model = GetAnimatedModel(startframe, numframes, framerate);
            return new Collada.Exporter("." + Path.DirectorySeparatorChar + Name + ".anim." + animname + ".dae", () => model.IsAnimated, () => model.GetFiltered(), () => model.RootJoint, model.CreatTime, model.ModTime, overwrite);
        }

        public Collada.Exporter<T> CreateTrimmedAnimatedExporter<T>(string animname, int startframe, int numframes, double framerate, bool overwrite, T state)
        {
            Model model = GetAnimatedModel(startframe, numframes, framerate);
            return new Collada.Exporter<T>("." + Path.DirectorySeparatorChar + Name + ".anim." + animname + ".dae", () => model.IsAnimated, () => model.GetFiltered(), () => model.RootJoint, model.CreatTime, model.ModTime, overwrite, state);
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

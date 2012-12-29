using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.IO;

namespace RS5_Extractor
{
    public class Collada : XDocument
    {
        protected class IDREF
        {
            public string ID { get; set; }

            public IDREF(string id)
            {
                this.ID = id;
            }
        }

        protected interface IXElementWithSID
        {
            string SID { get; }
        }

        protected interface IInstanceable
        {
            InstanceBase GetInstance();
        }

        protected abstract class XElementWithID : XElement
        {
            public string ID { get; protected set; }

            public XElementWithID(XName name, string ID, params object[] elements)
                : base(name, new XAttribute("id", ID), elements)
            {
                this.ID = ID;
            }

            public XElementWithID(XName name, string ID, IEnumerable<XElement> elements)
                : base(name, new XAttribute("id", ID), elements)
            {
                this.ID = ID;
            }
        }

        protected abstract class InstanceBase : XElement
        {
            protected InstanceBase(XName name)
                : base(name)
            {
            }
        }

        protected class Animation : XElementWithID
        {
            protected class Sampler : XElementWithID
            {
                public Sampler(string ID, params Source[] sources)
                    : base(ns + "sampler", ID, sources.Select(s => s.GetInput()))
                {
                }
            }

            protected class Channel : XElement
            {
                public Channel(XElementWithID source, string target)
                    : base(ns + "channel")
                {
                    this.Add(
                        new XAttribute("source", "#" + source.ID),
                        new XAttribute("target", target)
                    );
                }
            }
            
            protected Animation(string ID) 
                : base(ns + "animation", ID)
            {
            }

            public Animation(string animationname, Bone bone)
                : this(animationname + "_" + bone.Joint.Symbol)
            {
                Source insrc = new Source(ID + "_time", "INPUT", "TIME", bone.Joint.Animation.Frames.Select(f => f.FrameNum / bone.Joint.Animation.FrameRate), bone.Joint.Animation.Frames.Length);
                Source outsrc = new Source(ID + "_out_xfrm", "OUTPUT", "TRANSFORM", bone.Joint.Animation.Frames.Select(f => f.Transform), bone.Joint.Animation.Frames.Length);
                Source intsrc = new Source(ID + "_interp", "INTERPOLATION", "INTERPOLATION", bone.Joint.Animation.Frames.Select(kvp => "LINEAR"), bone.Joint.Animation.Frames.Length);
                Sampler sampler = new Sampler(ID + "_xfrm", insrc, outsrc, intsrc);

                this.Add(
                    //new XAttribute("name", skeleton.Joint.Symbol),
                    insrc,
                    outsrc,
                    intsrc,
                    sampler,
                    new Channel(sampler, bone.ID + "/" + bone.Transform.SID)
                );
            }

            public Animation(Skeleton skeleton) 
                : this(skeleton.SkeletonName + "_anim")
            {
                this.Add(skeleton.GetAnimatedBones().Select(b => new Animation(ID, b)));
            }
        }

        protected class SkinController : XElementWithID, IInstanceable
        {
            protected Skeleton Skeleton { get; set; }
            protected Geometry Geometry { get; set; }

            protected class Instance : InstanceBase
            {
                public Instance(SkinController controller, Skeleton skeleton, Geometry geometry)
                    : base(ns + "instance_controller")
                {
                    this.Add(
                        new XAttribute("url", "#" + controller.ID),
                        new XElement(ns + "skeleton", "#" + skeleton.ID),
                        geometry.GetMaterialBinding()
                    );
                }
            }

            public SkinController(Geometry geometry, Skeleton skeleton)
                : base(ns + "controller", geometry.ID + "_skin")
            {
                this.Skeleton = skeleton;
                this.Geometry = geometry;
                List<Bone> bones = skeleton.GetSelfAndDescendents().ToList();
                List<string> jntsyms = bones.Select(b => b.Joint.Symbol).ToList();
                Dictionary<string, int> jntrevindex = jntsyms.Select((s, i) => new KeyValuePair<string, int>(s, i)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                Source jointsrc = new Source(ID + "_jnt", "JOINT", "JOINT", bones.Select(b => new IDREF(b.ID)), bones.Count);
                Source bindsrc = new Source(ID + "_bnd", "INV_BIND_MATRIX", "TRANSFORM", bones.Select(b => b.Joint.ReverseBindingMatrix), bones.Count);
                Source weightsrc = new Source(ID + "_wgt", "WEIGHT", "WEIGHT", Enumerable.Range(0, 256).Select(v => v / 255.0), 256);

                this.Add(
                    //new XAttribute("name", ID),
                    new XElement(ns + "skin",
                        new XAttribute("source", "#" + geometry.ID),
                        new XElement(ns + "bind_shape_matrix", GetString(Matrix4.Identity)),
                        jointsrc,
                        bindsrc,
                        weightsrc,
                        new XElement(ns + "joints",
                            jointsrc.GetInput(),
                            bindsrc.GetInput()
                        ),
                        new XElement(ns + "vertex_weights",
                            new XAttribute("count", geometry.VertexList.Count()),
                            jointsrc.GetInput(0),
                            weightsrc.GetInput(1),
                            ValueList(geometry.VertexList.Select(v => v.JointInfluence.Select(j => new int[] { jntrevindex[j.Joint.Symbol], (int)((j.Influence * 255.0) + 0.25) })))
                        )
                    )
                );
            }

            public InstanceBase GetInstance()
            {
                return new Instance(this, Skeleton, Geometry);
            }
        }

        protected class Matrix : XElement, IXElementWithSID
        {
            public string SID { get; protected set; }
            public Matrix4 Matrix4 { get; protected set; }

            public Matrix(string SID, Matrix4 matrix)
                : base(ns + "matrix")
            {
                this.SID = SID;
                this.Matrix4 = matrix;
                this.Add(
                    new XAttribute("sid", SID),
                    GetString(matrix)
                );
            }
        }

        protected class Node : XElementWithID, IInstanceable
        {
            protected class Instance : InstanceBase
            {
                public Instance(Node node)
                    : base(ns + "instance_node")
                {
                    this.Add(
                        new XAttribute("url", "#" + node.ID)
                    );
                }
            }

            public Node(string ID, params object[] content)
                : base(ns + "node", ID, content)
            {
            }

            public InstanceBase GetInstance()
            {
                return new Instance(this);
            }
        }

        protected class Bone : Node, IXElementWithSID
        {

            public string SID { get; protected set; }
            public Joint Joint { get; protected set; }
            public Bone[] ChildBones { get; protected set; }
            public Matrix Transform { get; protected set; }

            protected Bone(string skeletonname, Joint joint, params object[] content)
                : base(skeletonname + "_" + joint.Symbol, content)
            {
                this.SID = joint.Symbol;
                this.Joint = joint;
                this.ChildBones = joint.Children.Select(j => new Bone(skeletonname, j)).ToArray();
                this.Transform = new Matrix("transform", joint.InitialPose);

                this.Add(
                    new XAttribute("sid", SID),
                    new XAttribute("type", "JOINT"),
                    Transform,
                    ChildBones
                );
            }

            public IEnumerable<Bone> GetSelfAndDescendents()
            {
                yield return this;
                foreach (Skeleton child in ChildBones)
                {
                    foreach (Skeleton descendent in child.GetSelfAndDescendents())
                    {
                        yield return descendent;
                    }
                }
            }

            public IEnumerable<Bone> GetAnimatedBones()
            {
                return GetSelfAndDescendents().Where(b => b.Joint.Animation.Frames.Length != 0);
            }
        }

        protected class Skeleton : Bone
        {
            public string SkeletonName { get; protected set; }

            public Skeleton(string skeletonname, Joint joint)
                : base(skeletonname, joint, new XAttribute("name", skeletonname))
            {
                this.SkeletonName = skeletonname;
            }
        }

        protected class Source : XElementWithID
        {
            protected string Semantic;

            protected Source(string sourcename, string semantic)
                : base(Collada.ns + "source", sourcename)
            {
                this.Semantic = semantic;
            }

            public Source(string sourcename, string semantic, string paramname, IEnumerable<string> values, int count)
                : this(sourcename, semantic)
            {
                this.Add(
                    new XElement(ns + "Name_array",
                        new XAttribute("id", sourcename + "_array"),
                        new XAttribute("count", count),
                        String.Join(" ", values)
                    ),
                    new XElement(ns + "technique_common",
                        new XElement(ns + "accessor",
                            new XAttribute("source", "#" + sourcename + "_array"),
                            new XAttribute("count", count),
                            new XAttribute("stride", 1),
                            new XElement(ns + "param",
                                new XAttribute("name", paramname),
                                new XAttribute("type", "Name")
                            )
                        )
                    )
                );
            }

            public Source(string sourcename, string semantic, string paramname, IEnumerable<IDREF> values, int count)
                : this(sourcename, semantic)
            {
                this.Add(
                    new XElement(ns + "IDREF_array",
                        new XAttribute("id", sourcename + "_array"),
                        new XAttribute("count", count),
                        String.Join(" ", values.Select(v => v.ID))
                    ),
                    new XElement(ns + "technique_common",
                        new XElement(ns + "accessor",
                            new XAttribute("source", "#" + sourcename + "_array"),
                            new XAttribute("count", count),
                            new XAttribute("stride", 1),
                            new XElement(ns + "param",
                                new XAttribute("name", paramname),
                                new XAttribute("type", "IDREF")
                            )
                        )
                    )
                );
            }

            public Source(string sourcename, string semantic, string paramname, IEnumerable<double> values, int count)
                : this(sourcename, semantic)
            {
                this.Add(
                    new XElement(ns + "float_array",
                        new XAttribute("id", sourcename + "_array"),
                        new XAttribute("count", count),
                        String.Join(" ", values.Select(v => String.Format("{0,8:F5}", v)))
                    ),
                    new XElement(ns + "technique_common",
                        new XElement(ns + "accessor",
                            new XAttribute("source", "#" + sourcename + "_array"),
                            new XAttribute("count", count),
                            new XAttribute("stride", 1),
                            new XElement(ns + "param",
                                new XAttribute("name", paramname),
                                new XAttribute("type", "float")
                            )
                        )
                    )
                );
            }

            public Source(string sourcename, string semantic, string paramname, IEnumerable<Matrix4> matrices, int count)
                : this(sourcename, semantic)
            {
                this.Add(
                    new XElement(ns + "float_array",
                        new XAttribute("id", sourcename + "_array"),
                        new XAttribute("count", count * 16),
                        "\n",
                        String.Join("\n", matrices.Select(m => GetString(m))),
                        "\n"
                    ),
                    new XElement(ns + "technique_common",
                        new XElement(ns + "accessor",
                            new XAttribute("source", "#" + sourcename + "_array"),
                            new XAttribute("count", count),
                            new XAttribute("stride", 16),
                            new XElement(ns + "param",
                                new XAttribute("name", paramname),
                                new XAttribute("type", "float4x4")
                            )
                        )
                    )
                );
            }

            public Source(string sourcename, string semantic, IEnumerable<Vector4> vectors, int count)
                : this(sourcename, semantic)
            {
                this.Add(
                    new XElement(ns + "float_array",
                        new XAttribute("id", sourcename + "_array"),
                        new XAttribute("count", count * 3),
                        "\n",
                        String.Join("\n", vectors.Select(v => String.Format("{0,9:F6} {1,9:F6} {2,9:F6}", v.X, v.Y, v.Z))),
                        "\n"
                    ),
                    new XElement(ns + "technique_common",
                        new XElement(ns + "accessor",
                            new XAttribute("count", count),
                            new XAttribute("offset", 0),
                            new XAttribute("source", "#" + sourcename + "_array"),
                            new XAttribute("stride", 3),
                            new XElement(ns + "param",
                                new XAttribute("name", "X"),
                                new XAttribute("type", "float")
                            ),
                            new XElement(ns + "param",
                                new XAttribute("name", "Y"),
                                new XAttribute("type", "float")
                            ),
                            new XElement(ns + "param",
                                new XAttribute("name", "Z"),
                                new XAttribute("type", "float")
                            )
                        )
                    )
                );
            }

            public Input GetInput(int? offset, int? set)
            {
                return new Input(ID, Semantic, offset, set);
            }

            public Input GetInput(int? offset)
            {
                return GetInput(offset, null);
            }

            public Input GetInput()
            {
                return GetInput(null);
            }
        }

        protected class TexCoordSource : Source
        {
            public List<Material> Materials { get; protected set; }
            protected Dictionary<string, int> Sets { get; set; }

            public TexCoordSource(string sourcename, string semantic, IEnumerable<TextureCoordinate> texcoords, int count, List<Material> materials)
                : base(sourcename, semantic)
            {
                this.Materials = materials;
                this.Sets = materials.Select((m, i) => new KeyValuePair<string, int>(m.ID, i)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                this.Add(
                    new XElement(ns + "float_array",
                        new XAttribute("id", sourcename + "_array"),
                        new XAttribute("count", count * 2),
                        "\n",
                        String.Join("\n", texcoords.Select(t => String.Format("{0,9:F6} {1,9:F6}", t.U, t.V))),
                        "\n"
                    ),
                    new XElement(ns + "technique_common",
                        new XElement(ns + "accessor",
                            new XAttribute("count", count),
                            new XAttribute("offset", 0),
                            new XAttribute("source", "#" + sourcename + "_array"),
                            new XAttribute("stride", 2),
                            new XElement(ns + "param",
                                new XAttribute("name", "S"),
                                new XAttribute("type", "float")
                            ),
                            new XElement(ns + "param",
                                new XAttribute("name", "T"),
                                new XAttribute("type", "float")
                            )
                        )
                    )
                );
            }

            public int? GetSetNum(Material material)
            {
                return Sets.ContainsKey(material.ID) ? (int?)Sets[material.ID] : null;
            }

            public Input GetInput(int? offset, Material material)
            {
                int? set = GetSetNum(material);
                return set == null ? null : new Input(ID, Semantic, offset, set);
            }

            public IEnumerable<InstanceBase> GetMaterialInstances()
            {
                return Materials.Where(m => Sets.ContainsKey(m.ID)).Select(m => m.GetInstance(Sets[m.ID]));
            }
        }

        protected class Input : XElement
        {
            protected Input()
                : base(ns + "input")
            {
            }

            public Input(string sourcename, string semantic, int? offset, int? set)
                : this()
            {
                this.Add(
                    new XAttribute("source", "#" + sourcename),
                    new XAttribute("semantic", semantic),
                    offset == null ? null : new XAttribute("offset", offset),
                    set == null ? null : new XAttribute("set", set)
                );
            }
        }

        protected class Geometry : XElementWithID, IInstanceable
        {
            public string Symbol { get; protected set; }
            public List<Vertex> VertexList { get; protected set; }
            protected Source PositionSource { get; set; }
            protected TexCoordSource TexcoordSource { get; set; }
            protected Source NormalSource { get; set; }
            protected Source TangentSource { get; set; }
            protected Source BinormalSource { get; set; }
            protected VerticesSource VertexSource { get; set; }
            protected List<Material> Materials { get; set; }

            protected class Instance : InstanceBase
            {
                protected Instance()
                    : base(ns + "instance_geometry")
                {
                }

                public Instance(Geometry geometry)
                    : this()
                {
                    this.Add(
                        new XAttribute("url", "#" + geometry.ID),
                        geometry.GetMaterialBinding()
                    );
                }
            }

            protected class VerticesSource : XElement
            {
                public string Symbol { get; protected set; }
                public string Semantic { get; protected set; }

                protected VerticesSource()
                    : base(ns + "vertices")
                {
                }

                public VerticesSource(string symbol, string semantic, params Source[] sources)
                    : this()
                {
                    this.Symbol = symbol;
                    this.Semantic = semantic;

                    this.Add(new XAttribute("id", symbol), sources.Select(v => v == null ? null : v.GetInput()));
                }

                public Input GetInput(int? offset, int? set)
                {
                    return new Input(Symbol, Semantic, offset, set);
                }

                public Input GetInput(int? offset)
                {
                    return GetInput(offset, null);
                }

                public Input GetInput()
                {
                    return GetInput(null);
                }
            }

            public Geometry(Collada document, string geometryname, List<Triangle> triangles)
                : base(ns + "geometry", geometryname)
            {
                this.Symbol = geometryname;
                Dictionary<string, List<Triangle>> trilists = new Dictionary<string,List<Triangle>>();
                this.VertexList = new List<Vertex>();
                this.Materials = new List<Material>();
                Dictionary<object, int> vtxrevindex = new Dictionary<object, int>();

                foreach (Triangle triangle in triangles)
                {
                    if (!trilists.ContainsKey(triangle.Texture.Name))
                    {
                        trilists[triangle.Texture.Name] = new List<Triangle>();
                        Material material = document.GetOrCreateMaterial(triangle.Texture);
                        Materials.Add(material);
                    }
                    trilists[triangle.Texture.Name].Add(triangle);
                    foreach (Vertex vertex in new Vertex[] { triangle.A, triangle.B, triangle.C })
                    {
                        if (!vtxrevindex.ContainsKey(vertex))
                        {
                            vtxrevindex[vertex] = VertexList.Count;
                            VertexList.Add(vertex);
                        }
                    }
                }

                PositionSource = new Source(geometryname + "_pos", "POSITION", VertexList.Select(v => v.Position), VertexList.Count);
                TexcoordSource = new TexCoordSource(geometryname + "_tex", "TEXCOORD", VertexList.Select(v => v.TexCoord), VertexList.Count, Materials);
                NormalSource = VertexList[0].Normal != Vector4.Zero ? new Source(geometryname + "_normal", "NORMAL", VertexList.Select(v => v.Normal), VertexList.Count) : null;
                TangentSource = VertexList[0].Tangent != Vector4.Zero ? new Source(geometryname + "_tangent", "TANGENT", VertexList.Select(v => v.Tangent), VertexList.Count) : null;
                BinormalSource = VertexList[0].Binormal != Vector4.Zero ? new Source(geometryname + "_binormal", "BINORMAL", VertexList.Select(v => v.Binormal), VertexList.Count) : null;
                VertexSource = new VerticesSource(geometryname + "_vtx", "VERTEX", PositionSource, NormalSource, TangentSource, BinormalSource);

                this.Add(
                    new XElement(ns + "mesh",
                        PositionSource,
                        TexcoordSource,
                        NormalSource,
                        TangentSource,
                        BinormalSource,
                        VertexSource,
                        Materials.Select(m =>
                            new XElement(ns + "triangles",
                                new XAttribute("count", trilists[m.Effect.Image.Texture.Name].Count),
                                new XAttribute("material", m.ID),
                                VertexSource.GetInput(0),
                                TexcoordSource.GetInput(1, m),
                                new XElement(ns + "p",
                                    "\n",
                                    String.Join("\n", trilists[m.Effect.Image.Texture.Name].Select(t => String.Join("  ", new Vertex[] { t.A, t.B, t.C }.Select(v => String.Format("{0} {0}", vtxrevindex[v]))))),
                                    "\n"
                                )
                            )
                        )
                    )
                );
            }

            public XElement GetMaterialBinding()
            {
                return new XElement(ns + "bind_material",
                    new XElement(ns + "technique_common",
                        TexcoordSource.GetMaterialInstances()
                    )
                );
            }

            public InstanceBase GetInstance()
            {
                return new Instance(this);
            }
        }

        protected class Image : XElementWithID
        {
            public string TextureSymbol { get; protected set; }
            public Texture Texture { get; protected set; }

            public Image(string texsym, Texture texture, string path)
                : base(ns + "image", texsym + "_img")
            {
                this.Texture = texture;
                this.TextureSymbol = texsym;

                this.Add(
                    new XAttribute("depth", "1"),
                    new XElement(ns + "init_from", (path + texture.Filename).Replace('\\', '/'))
                );
            }
        }

        protected class Effect : XElementWithID, IInstanceable
        {
            public Image Image { get; protected set; }

            protected class NewParam : XElement, IXElementWithSID
            {
                public string SID { get; protected set; }
                protected XElement Child { get; set; }

                public NewParam(string sid, XName childname)
                    : base(ns + "newparam")
                {
                    this.SID = sid;
                    this.Child = new XElement(childname);
                    base.Add(
                        new XAttribute("sid", SID),
                        Child
                    );
                }

                protected new void Add(params object[] content)
                {
                    Child.Add(content);
                }
            }
            
            protected class Surface : NewParam
            {
                public Image Texture { get; protected set; }

                public Surface(Image texture)
                    : base(texture.TextureSymbol + "_sfc", ns + "surface")
                {
                    this.Texture = texture;
                    this.Add(
                        new XAttribute("type", "2D"),
                        new XElement(ns + "init_from", texture.ID),
                        new XElement(ns + "format", "A8R8G8B8")
                    );
                }
            }

            protected class Sampler2D : NewParam
            {
                public Surface Surface { get; protected set; }

                public Sampler2D(Surface surface)
                    : base(surface.Texture.TextureSymbol + "_smp", ns + "sampler2D")
                {
                    this.Surface = surface;
                    this.Add(
                        new XElement(ns + "source", surface.SID),
                        new XElement(ns + "minfilter", "LINEAR_MIPMAP_LINEAR"),
                        new XElement(ns + "magfilter", "LINEAR")
                    );
                }
            }

            protected class Instance : InstanceBase
            {
                public Instance(string ID)
                    : base(ns + "instance_effect")
                {
                    this.Add(
                        new XAttribute("url", "#" + ID)
                    );
                }
            }

            protected Effect(string ID)
                : base(ns + "effect", ID)
            {
            }

            public Effect(Image texture)
                : this(texture.TextureSymbol + "_fx")
            {
                this.Image = texture;
                Surface surface = new Surface(texture);
                Sampler2D sampler = new Sampler2D(surface);

                this.Add(
                    new XElement(ns + "profile_COMMON",
                        surface,
                        sampler,
                        new XElement(ns + "technique",
                            new XAttribute("sid", "common"),
                            new XElement(ns + "blinn",
                                new XElement(ns + "emission", new XElement(ns + "color", "0 0 0 1")),
                                new XElement(ns + "ambient", new XElement(ns + "color", "0 0 0 1")),
                                new XElement(ns + "diffuse",
                                    new XElement(ns + "texture",
                                        new XAttribute("texture", sampler.SID),
                                        new XAttribute("texcoord", "TEXCOORD")
                                    )
                                ),
                                new XElement(ns + "specular", new XElement(ns + "color", "0 0 0 1")),
                                new XElement(ns + "shininess", new XElement(ns + "float", "0.2")),
                                new XElement(ns + "reflective", new XElement(ns + "color", "0 0 0 1")),
                                new XElement(ns + "reflectivity", new XElement(ns + "float", "0.2")),
                                new XElement(ns + "transparent",
                                    new XAttribute("opaque", "A_ONE"),
                                    new XElement(ns + "texture",
                                        new XAttribute("texture", sampler.SID),
                                        new XAttribute("texcoord", "TEXCOORD")
                                    )
                                ),
                                new XElement(ns + "transparency", new XElement(ns + "float", "1.0")),
                                new XElement(ns + "index_of_refraction", new XElement(ns + "float", "1.0"))
                            )
                        )
                    )
                );
            }
            
            public InstanceBase GetInstance()
            {
                return new Instance(ID);
            }
        }

        protected class Material : XElementWithID
        {
            public Effect Effect { get; protected set; }

            protected class Instance : InstanceBase
            {
                protected Instance()
                    : base(ns + "instance_material")
                {
                }

                public Instance(Material material, int set)
                    : this()
                {
                    this.Add(
                        new XAttribute("symbol", material.ID),
                        new XAttribute("target", "#" + material.ID),
                        new XElement(ns + "bind_vertex_input",
                            new XAttribute("semantic", "TEXCOORD"),
                            new XAttribute("input_semantic", "TEXCOORD"),
                            new XAttribute("input_set", set.ToString())
                        )
                    );
                }
            }

            public Material(Effect effect)
                : base(ns + "material", effect.Image.TextureSymbol + "_mtl", effect.GetInstance())
            {
                this.Effect = effect;
            }

            public InstanceBase GetInstance(int set)
            {
                return new Instance(this, set);
            }
        }

        public class Exporter
        {
            public readonly string Filename;
            public readonly Func<bool> IsValidFactory;
            public readonly Func<IEnumerable<List<Triangle>>> MeshFactory;
            public readonly Func<Joint> SkeletonFactory;
            public readonly DateTime CreateTime;
            public readonly DateTime ModTime;
            public readonly bool overwrite;

            public Exporter(string filename, Func<bool> IsValidFactory, Func<IEnumerable<List<Triangle>>> MeshFactory, Func<Joint> SkeletonFactory, DateTime CreateTime, DateTime ModTime, bool overwrite)
            {
                this.Filename = filename;
                this.IsValidFactory = IsValidFactory;
                this.MeshFactory = MeshFactory;
                this.SkeletonFactory = SkeletonFactory;
                this.CreateTime = CreateTime;
                this.ModTime = ModTime;
                this.overwrite = overwrite;
            }

            public void Save(Action onsave, Action oncomplete)
            {
                if (IsValidFactory() && (overwrite || !File.Exists(Filename)))
                {
                    onsave();
                    Collada.Save(Filename, MeshFactory(), SkeletonFactory(), CreateTime, ModTime);
                    oncomplete();
                }
            }
        }
        
        public class Exporter<T> : Exporter
        {
            public readonly T ObjectState;

            public Exporter(string filename, Func<bool> IsValidFactory, Func<IEnumerable<List<Triangle>>> MeshFactory, Func<Joint> SkeletonFactory, DateTime CreateTime, DateTime ModTime, bool overwrite, T state)
                : base(filename, IsValidFactory, MeshFactory, SkeletonFactory, CreateTime, ModTime, overwrite)
            {
                this.ObjectState = state;
            }

            public void Save(Action<T> onsave, Action<T> oncomplete)
            {
                if (IsValidFactory() && (overwrite || !File.Exists(Filename)))
                {
                    onsave(ObjectState);
                    Collada.Save(Filename, MeshFactory(), SkeletonFactory(), CreateTime, ModTime);
                    oncomplete(ObjectState);
                }
            }
        }

        protected static XNamespace ns = "http://www.collada.org/2005/11/COLLADASchema";

        protected static string GetString(Matrix4 matrix)
        {
            return String.Join(" ", Enumerable.Range(0, 16).Select(i => String.Format("{0,9:F6}", matrix[i])));
        }

        protected static IEnumerable<XNode> ValueList(IEnumerable<IEnumerable<int[]>> values)
        {
            List<int> vcount = new List<int>();
            List<List<int[]>> v = new List<List<int[]>>();
            foreach (IEnumerable<int[]> vals in values)
            {
                List<int[]> vlist = vals.ToList();
                v.Add(vlist);
                vcount.Add(vlist.Count());
            }
            yield return new XElement(ns + "vcount", String.Join(" ", vcount));
            yield return new XElement(ns + "v",
                "\n",
                String.Join("\n", v.Select(vlist => String.Join("  ", vlist.Select(vals => String.Join(" ", vals))))),
                "\n"
            );
        }

        protected string basepath;
        
        protected Dictionary<string, int> SymbolNumbers = new Dictionary<string,int>();
        protected Dictionary<string, Material> Materials = new Dictionary<string,Material>();

        protected string CreateSymbol(string prefix)
        {
            lock(SymbolNumbers)
            {
                if (!SymbolNumbers.ContainsKey(prefix))
                {
                    SymbolNumbers[prefix] = 0;
                }
                return String.Format("{0}{1}", prefix, SymbolNumbers[prefix]++);
            }
        }

        protected Material GetOrCreateMaterial(Texture texture)
        {
            if (Materials.ContainsKey(texture.Name))
            {
                return Materials[texture.Name];
            }
            else
            {
                Material material = new Material(new Effect(new Image(CreateSymbol("texture"), texture, basepath)));
                Materials[texture.Name] = material;
                return material;
            }
        }

        public Collada(IEnumerable<List<Triangle>> meshes, Joint rootjoint, string path, DateTime creattime, DateTime modtime)
        {
            basepath = path;
            List<Geometry> geometries = new List<Geometry>();
            List<Node> scenenodes = new List<Node>();
            List<Node> modelnodes = new List<Node>();
            List<SkinController> controllers = new List<SkinController>();
            Skeleton skeleton = rootjoint == null ? null : new Skeleton("model_skel", rootjoint);
            Animation animation = rootjoint == null ? null : skeleton.GetAnimatedBones().Count() != 0 ? new Animation(skeleton) : null;
            List<InstanceBase> instnodes = new List<InstanceBase>();

            foreach (List<Triangle> mesh in meshes.Where(m => m.Count != 0))
            {
                Geometry geometry = new Geometry(this, CreateSymbol("model_mesh"), mesh);
                Node node = new Node(geometry.ID + "_node", geometry.GetInstance());
                geometries.Add(geometry);
                if (skeleton != null)
                {
                    SkinController controller = new SkinController(geometry, skeleton);
                    controllers.Add(controller);
                    node = new Node(controller.ID + "_node", controller.GetInstance());
                }
                modelnodes.Add(node);
            }

            Node modelroot = new Node("model", modelnodes, skeleton);
            scenenodes.Add(modelroot);

            this.Add(
                new XElement(ns + "COLLADA",
                    new XAttribute("version", "1.4.1"),
                    new XElement(ns + "asset",
                        new XElement(ns + "contributor",
                            new XElement(ns + "author", "IonFx")
                        ),
                        new XElement(ns + "created", creattime.ToString("O")),
                        new XElement(ns + "modified", modtime.ToString("O")),
                        new XElement(ns + "up_axis", "Z_UP")
                    ),
                    new XElement(ns + "library_images", Materials.Values.Select(m => m.Effect.Image)),
                    new XElement(ns + "library_effects", Materials.Values.Select(m => m.Effect)),
                    new XElement(ns + "library_materials", Materials.Values),
                    new XElement(ns + "library_geometries", geometries),
                    controllers.Count == 0 ? null : new XElement(ns + "library_controllers", controllers),
                    animation == null ? null : new XElement(ns + "library_animations", animation),
                    new XElement(ns + "library_visual_scenes",
                        new XElement(ns + "visual_scene",
                            new XAttribute("id", "visual_scene"),
                            scenenodes
                        )
                    ),
                    new XElement(ns + "scene",
                        new XElement(ns + "instance_visual_scene",
                            new XAttribute("url", "#visual_scene")
                        )
                    )
                )
            );
        }

        public static void Save(string filename, IEnumerable<List<Triangle>> meshes, Joint rootjoint, DateTime creattime, DateTime modtime)
        {
            string dir = Path.GetDirectoryName(filename);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string path = "." + Path.DirectorySeparatorChar + String.Join(Path.DirectorySeparatorChar.ToString(), Path.GetDirectoryName(filename).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Where(d => d != "." && d != "..").Select(d => ".."));
            
            XDocument doc = new Collada(meshes, rootjoint, path, creattime, modtime);
            doc.Save(filename);
            File.SetLastWriteTimeUtc(filename, modtime);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RS5_Extractor
{
    public class Collada
    {
        protected static XNamespace ns = "http://www.collada.org/2005/11/COLLADASchema";

        protected static XNode Animation(string animationname, string skeletonname, string seqname, AnimationSequence sequence, int startframe, int numframes, float framerate)
        {
            string seqid = animationname + "_" + seqname;
            AnimationSequence seq = sequence.Trim(startframe, numframes);

            return new XElement(ns + "animation",
                new XAttribute("id", seqid),
                new XAttribute("name", seqname),
                new XElement(ns + "source",
                    new XAttribute("id", seqid + "_time"),
                    new XElement(ns + "float_array",
                        new XAttribute("id", seqid + "_time_array"),
                        new XAttribute("count", seq.Frames.Count),
                        String.Join(" ", seq.Frames.Keys.Select(k => String.Format("{0,7:F3}", k / framerate)))
                    ),
                    new XElement(ns + "technique_common",
                        new XElement(ns + "accessor",
                            new XAttribute("source", "#" + seqid + "_time_array"),
                            new XAttribute("count", seq.Frames.Count),
                            new XAttribute("stride", 1),
                            new XElement(ns + "param",
                                new XAttribute("name", "TIME"),
                                new XAttribute("type", "float")
                            )
                        )
                    )
                ),
                Source(seqid + "_out_xfrm", seq.Frames.Values, seq.Frames.Count),
                new XElement(ns + "source",
                    new XAttribute("id", seqid + "_interp"),
                    new XElement(ns + "Name_array",
                        new XAttribute("id", seqid + "_interp_array"),
                        new XAttribute("count", seq.Frames.Count),
                        String.Join(" ", Enumerable.Range(0, seq.Frames.Count).Select(i => "LINEAR"))
                    ),
                    new XElement(ns + "technique_common",
                        new XElement(ns + "accessor",
                            new XAttribute("source", "#" + seqid + "_interp_array"),
                            new XAttribute("count", seq.Frames.Count),
                            new XAttribute("stride", 1),
                            new XElement(ns + "param",
                                new XAttribute("name", "INTERPOLATION"),
                                new XAttribute("type", "name")
                            )
                        )
                    )
                ),
                new XElement(ns + "sampler",
                    new XAttribute("id", seqid + "_xfrm"),
                    new XElement(ns + "input",
                        new XAttribute("semantic", "INPUT"),
                        new XAttribute("source", "#" + seqid + "_time")
                    ),
                    new XElement(ns + "input",
                        new XAttribute("semantic", "OUTPUT"),
                        new XAttribute("source", "#" + seqid + "_out_xfrm")
                    ),
                    new XElement(ns + "input",
                        new XAttribute("semantic", "INTERPOLATION"),
                        new XAttribute("source", "#" + seqid + "_interp")
                    )
                ),
                new XElement(ns + "channel",
                    new XAttribute("source", "#" + seqid + "_xfrm"),
                    new XAttribute("target", skeletonname + "_" + seqname + "/transform.MATRIX")
                )
            );
        }

        protected static XNode Animation(string animationname, string skeletonname, Dictionary<string, AnimationSequence> sequences, int startframe, int numframes, float framerate)
        {
            return new XElement(ns + "animation", sequences.Select(kvp => Animation(animationname, skeletonname, kvp.Key, kvp.Value, startframe, numframes, framerate)));
        }

        protected static XNode SkinController(string geometryname, string skeletonname, string skinname, List<Vertex> vertices, Dictionary<string, Joint> joints)
        {
            List<string> jntsyms = joints.Keys.ToList();
            Dictionary<string, int> jntrevindex = jntsyms.Select((s, i) => new KeyValuePair<string, int>(s, i)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            return new XElement(ns + "controller",
                new XAttribute("id", skinname),
                new XAttribute("name", skinname),
                new XElement(ns + "skin",
                    new XAttribute("source", "#" + geometryname),
                    new XElement(ns + "bind_shape_matrix", "1 0 0 0  0 1 0 0  0 0 1 0  0 0 0 1"),
                    new XElement(ns + "source",
                        new XAttribute("id", skinname + "_jnt"),
                        new XElement(ns + "IDREF_array",
                            new XAttribute("id", skinname + "_jnt_array"),
                            new XAttribute("count", joints.Count.ToString()),
                            String.Join(" ", joints.Select(kvp => skeletonname + "_" + kvp.Key))
                        ),
                        new XElement(ns + "technique_common",
                            new XElement(ns + "accessor",
                                new XAttribute("source", "#" + skinname + "_jnt_array"),
                                new XAttribute("count", joints.Count),
                                new XAttribute("stride", 1),
                                new XElement(ns + "param",
                                    new XAttribute("name", "JOINT"),
                                    new XAttribute("type", "IDREF")
                                )
                            )
                        )
                    ),
                    Source(skinname + "_bnd", joints.Select(kvp => kvp.Value.ReverseBindingMatrix), joints.Count),
                    new XElement(ns + "source",
                        new XAttribute("id", skinname + "_wgt"),
                        new XElement(ns + "float_array",
                            new XAttribute("id", skinname + "_wgt_array"),
                            new XAttribute("count", 256),
                            String.Join(" ", Enumerable.Range(0, 256).Select(v => String.Format("{0,5:F3}", v / 255.0)))
                        ),
                        new XElement(ns + "technique_common",
                            new XElement(ns + "accessor",
                                new XAttribute("source", "#" + skinname + "_wgt_array"),
                                new XAttribute("count", 256),
                                new XAttribute("stride", 1),
                                new XElement(ns + "param",
                                    new XAttribute("name", "WEIGHT"),
                                    new XAttribute("type", "float")
                                )
                            )
                        )
                    ),
                    new XElement(ns + "joints",
                        new XElement(ns + "input",
                            new XAttribute("semantic", "JOINT"),
                            new XAttribute("source", "#" + skinname + "_jnt")
                        ),
                        new XElement(ns + "input",
                            new XAttribute("semantic", "INV_BIND_MATRIX"),
                            new XAttribute("source", "#" + skinname + "_bnd")
                        )
                    ),
                    new XElement(ns + "vertex_weights",
                        new XAttribute("count", vertices.Count()),
                        new XElement(ns + "input",
                            new XAttribute("offset", 0),
                            new XAttribute("semantic", "JOINT"),
                            new XAttribute("source", "#" + skinname + "_jnt")
                        ),
                        new XElement(ns + "input",
                            new XAttribute("offset", 1),
                            new XAttribute("semantic", "WEIGHT"),
                            new XAttribute("source", "#" + skinname + "_wgt")
                        ),
                        new XElement(ns + "vcount",
                            String.Join(" ", vertices.Select(v => v.JointInfluence.Length))
                        ),
                        new XElement(ns + "v",
                            String.Join("\n", vertices.Select(v => String.Join(" ",v.JointInfluence.Select(j => String.Format("{0,3} {1,3}  ", jntrevindex[j.JointSymbol], (int)((j.Influence * 255.0) + 0.25))))))
                        )
                    )
                )
            );
        }

        protected static XNode Skeleton(string skeletonname, Joint joint, Matrix4 parentmatrix)
        {
            return new XElement(ns + "node",
                new XAttribute("id", skeletonname + "_" + joint.Symbol),
                new XAttribute("name", joint.Name),
                new XAttribute("sid", joint.Symbol),
                new XAttribute("type", "JOINT"),
                new XElement(ns + "matrix",
                    new XAttribute("sid", "transform"),
                    String.Join(" ", Enumerable.Range(0, 16).Select(i => String.Format("{0,8:F5}", joint.InitialPose[i])))
                ),
                joint.Children.Select(j => Skeleton(skeletonname, j, joint.ReverseBindingMatrix))
            );
        }

        protected static XNode BindMaterial(int texnum, string texname, Texture texture)
        {
            return new XElement(ns + "bind_material",
                new XElement(ns + "technique_common",
                    new XElement(ns + "instance_material",
                        new XAttribute("symbol", texname + "_lnk"),
                        new XAttribute("target", "#" + texname + "_mtl"),
                        new XElement(ns + "bind_vertex_input",
                            new XAttribute("semantic", "TEXCOORD"),
                            new XAttribute("input_semantic", "TEXCOORD"),
                            new XAttribute("input_set", String.Format("{0}", texnum))
                        )
                    )
                )
            );
        }

        protected static XNode NodeVisibility(Dictionary<string, Texture> textures, bool visible)
        {
            return new XElement(ns + "extra",
                new XElement(ns + "technique",
                    new XAttribute("profile", "FCOLLADA"),
                    new XElement(ns + "visibility", visible ? "1" : "0")
                ),
                new XElement(ns + "technique",
                    new XAttribute("profile", "XSI"),
                    new XElement(ns + "SI_Visibility",
                        new XElement(ns + "xsi_param",
                            new XAttribute("sid", "visibility"),
                            visible ? "TRUE" : "FALSE"
                        )
                    )
                )
            );
        }

        protected static XNode GeometryInstance(string modelname, string geometryname, Dictionary<string, Texture> textures, List<string> texsyms, bool visible)
        {
            return new XElement(ns + "node",
                new XAttribute("id", modelname),
                new XElement(ns + "instance_geometry",
                    new XAttribute("url", "#" + geometryname),
                    texsyms.Select((sym,i) => BindMaterial(i, sym, textures[sym]))
                ),
                NodeVisibility(textures, visible)
            );
        }

        protected static XNode SkinControllerInstance(string modelname, string skinname, string skeletonname, Dictionary<string, Texture> textures, List<string> texsyms)
        {
            return new XElement(ns + "node",
                new XAttribute("id", modelname),
                new XElement(ns + "instance_controller",
                    new XAttribute("url", "#" + skinname),
                    new XElement(ns + "skeleton", "#" + skeletonname),
                    textures.Select((kvp, i) => BindMaterial(i, kvp.Key, kvp.Value))
                )
            );
        }

        protected static XNode Source(string sourcename, IEnumerable<Matrix4> matrices, int count)
        {
            return new XElement(ns + "source",
                new XAttribute("id", sourcename),
                new XElement(ns + "float_array",
                    new XAttribute("id", sourcename + "_array"),
                    new XAttribute("count", count * 16),
                    "\n",
                    String.Join("\n", matrices.Select(m => String.Join(" ", Enumerable.Range(0, 16).Select(i => String.Format("{0,8:F5}", m[i]))))),
                    "\n"
                ),
                new XElement(ns + "technique_common",
                    new XElement(ns + "accessor",
                        new XAttribute("source", "#" + sourcename + "_array"),
                        new XAttribute("count", count),
                        new XAttribute("stride", 16),
                        new XElement(ns + "param",
                            new XAttribute("name", "TRANSFORM"),
                            new XAttribute("type", "float4x4")
                        )
                    )
                )
            );
        }
        
        protected static XNode Source(string sourcename, IEnumerable<Vector4> vectors, int count)
        {
            return new XElement(ns + "source",
                new XAttribute("id", sourcename),
                new XElement(ns + "float_array",
                    new XAttribute("id", sourcename + "_array"),
                    new XAttribute("count", count * 3),
                    "\n",
                    String.Join("\n",vectors.Select(v => String.Format("{0,12:F6} {1,12:F6} {2,12:F6}", v.X, v.Y, v.Z))),
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

        protected static XNode Source(string sourcename, IEnumerable<TextureCoordinate> texcoords, int count)
        {
            return new XElement(ns + "source",
                new XAttribute("id", sourcename),
                new XElement(ns + "float_array",
                    new XAttribute("id", sourcename + "_array"),
                    new XAttribute("count", count * 2),
                    "\n",
                    String.Join("\n", texcoords.Select(t => String.Format("{0,8:F5} {1,8:F5}", t.U, t.V))),
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

        protected static XNode Geometry(string geometryname, List<Triangle> triangles, Dictionary<string, Texture> textures, out List<Vertex> vertices, out List<string> texsyms)
        {
            vertices = new List<Vertex>();
            Dictionary<Vertex, int> vtxrevindex = new Dictionary<Vertex, int>();
            Dictionary<string, List<Triangle>> trilists = new Dictionary<string, List<Triangle>>();
            Dictionary<string, int> texrevindex = new Dictionary<string, int>();
            texsyms = new List<string>();

            int texnum = 0;
            foreach (KeyValuePair<string, Texture> texture_kvp in textures)
            {
                trilists.Add(texture_kvp.Key, new List<Triangle>());
                texsyms.Add(texture_kvp.Key);
                texrevindex[texture_kvp.Key] = texnum;
            }

            foreach (Triangle triangle in triangles)
            {
                if (trilists.ContainsKey(triangle.TextureSymbol))
                {
                    trilists[triangle.TextureSymbol].Add(triangle);
                    foreach (Vertex vertex in new Vertex[] { triangle.A, triangle.B, triangle.C })
                    {
                        if (!vtxrevindex.ContainsKey(vertex))
                        {
                            vtxrevindex[vertex] = vertices.Count;
                            vertices.Add(vertex);
                        }
                    }
                }
            }

            List<Vector4> positions = new List<Vector4>();
            List<TextureCoordinate> texcoords = new List<TextureCoordinate>();
            List<Vector4> normals = new List<Vector4>();
            List<Vector4> tangents = new List<Vector4>();
            List<Vector4> binormals = new List<Vector4>();
            int count = 0;
            bool hasnormals = vertices[0].Normal != Vector4.Zero;
            bool hastangents = vertices[0].Tangent != Vector4.Zero;
            bool hasbinormals = vertices[0].Binormal != Vector4.Zero;

            foreach (Vertex vertex in vertices)
            {
                positions.Add(vertex.Position);
                texcoords.Add(vertex.TexCoord);
                if (hasnormals) normals.Add(vertex.Normal);
                if (hastangents) tangents.Add(vertex.Tangent);
                if (hasbinormals) binormals.Add(vertex.Binormal);
                count++;
            }

            XNode positionxml = Source(geometryname + "_pos", positions, count);
            XNode texcoordxml = Source(geometryname + "_tex", texcoords, count);
            XNode normalxml = hasnormals ? Source(geometryname + "_normal", normals, count) : null;
            XNode tangentxml = hastangents ? Source(geometryname + "_tangent", tangents, count) : null;
            XNode binormalxml = hasbinormals ? Source(geometryname + "_binormal", binormals, count) : null;

            List<XNode> trianglexml = new List<XNode>();
            foreach (KeyValuePair<string, List<Triangle>> trilist_kvp in trilists)
            {
                trianglexml.Add(
                    new XElement(ns + "triangles",
                        new XAttribute("count", triangles.Count),
                        new XAttribute("material", trilist_kvp.Key + "_lnk"),
                        new XElement(ns + "input",
                            new XAttribute("offset", 0),
                            new XAttribute("semantic", "VERTEX"),
                            new XAttribute("source", "#" + geometryname + "_vtx")
                        ),
                        new XElement(ns + "input",
                            new XAttribute("offset", 1),
                            new XAttribute("semantic", "TEXCOORD"),
                            new XAttribute("source", "#" + geometryname + "_tex"),
                            new XAttribute("set", String.Format("{0}", texrevindex[trilist_kvp.Key]))
                        ),
                        new XElement(ns + "p",
                            String.Join(" ", trilist_kvp.Value.SelectMany(t => new Vertex[] { t.A, t.B, t.C }).Select(v => String.Format("{0} {0}", vtxrevindex[v])))
                        )
                    )
                );
            }

            return new XElement(ns + "geometry",
                new XAttribute("id", geometryname),
                new XElement(ns + "mesh",
                    positionxml,
                    texcoordxml,
                    normalxml,
                    tangentxml,
                    binormalxml,
                    new XElement(ns + "vertices",
                        new XAttribute("id", geometryname + "_vtx"),
                        new XElement(ns + "input",
                            new XAttribute("semantic", "POSITION"),
                            new XAttribute("source", "#" + geometryname + "_pos")
                        ),
                        hasnormals ? new XElement(ns + "input",
                            new XAttribute("semantic", "NORMAL"),
                            new XAttribute("source", "#" + geometryname + "_normal")
                        ) : null,
                        hastangents ? new XElement(ns + "input",
                            new XAttribute("semantic", "TANGENT"),
                            new XAttribute("source", "#" + geometryname + "_tangent")
                        ) : null,
                        hasbinormals ? new XElement(ns + "input",
                            new XAttribute("semantic", "BINORMAL"),
                            new XAttribute("source", "#" + geometryname + "_binormal")
                        ) : null
                    ),
                    trianglexml
                )
            );
        }

        protected static XNode Image(string texname, Texture texture, string path)
        {
            return new XElement(ns + "image",
                new XAttribute("id", texname + "_img"),
                new XAttribute("name", texname + "_img"),
                new XAttribute("depth", "1"),
                new XElement(ns + "init_from", String.Join("", path.Where(c => c == '\\').Select(c => "../")) + texture.PNGFilename.Replace('\\', '/'))
            );
        }

        protected static XNode Effect(string texname)
        {
            return new XElement(ns + "effect",
                new XAttribute("id", texname + "_fx"),
                new XElement(ns + "profile_COMMON",
                    new XElement(ns + "newparam",
                        new XAttribute("sid", texname + "_sfc"),
                        new XElement(ns + "surface",
                            new XAttribute("type", "2D"),
                            new XElement(ns + "init_from", texname + "_img"),
                            new XElement(ns + "format", "A8R8G8B8")
                        )
                    ),
                    new XElement(ns + "newparam",
                        new XAttribute("sid", texname + "_smp"),
                        new XElement(ns + "sampler2D",
                            new XElement(ns + "source", texname + "_sfc"),
                            new XElement(ns + "minfilter", "LINEAR_MIPMAP_LINEAR"),
                            new XElement(ns + "magfilter", "LINEAR")
                        )
                    ),
                    new XElement(ns + "technique",
                        new XAttribute("sid", "common"),
                        new XElement(ns + "blinn",
                            new XElement(ns + "emission", new XElement(ns + "color", "0 0 0 1")),
                            new XElement(ns + "ambient", new XElement(ns + "color", "0 0 0 1")),
                            new XElement(ns + "diffuse",
                                new XElement(ns + "texture",
                                    new XAttribute("texture", texname + "_smp"),
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
                                    new XAttribute("texture", texname + "_smp"),
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

        protected static XNode Material(string texname)
        {
            return new XElement(ns + "material",
                new XAttribute("id", texname + "_mtl"),
                new XElement(ns + "instance_effect",
                    new XAttribute("url", "#" + texname + "_fx")
                )
            );
        }

        public static XDocument Create(Model model, string[] exclude_texture_prefixes, bool skin, bool animate, int startframe, int numframes, float framerate)
        {
            List<XNode> geometries = new List<XNode>();
            List<XNode> scenenodes = new List<XNode>();
            List<XNode> libnodes = new List<XNode>();
            List<XNode> controllers = new List<XNode>();
            XNode animation = null;

            if (skin)
            {
                Dictionary<string, Texture> texlist = model.Textures.Where(kvp => exclude_texture_prefixes.Count(v => kvp.Value.Name.StartsWith(v)) == 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                List<Triangle> trilist = model.Triangles.Where(t => texlist.Values.Contains(t.Texture)).ToList();
                
                if (trilist.Count == 0)
                {
                    Console.WriteLine("  No visible geometry");
                    return null;
                }

                List<Vertex> vtxlist;
                List<string> texsyms = new List<string>();
                geometries.Add(Geometry("model_mesh", trilist, texlist, out vtxlist, out texsyms));

                if (model.RootJoint != null)
                {
                    controllers.Add(SkinController("model_mesh", "model_skel", "model_skin", vtxlist, model.Joints));
                    scenenodes.Add(SkinControllerInstance("model", "model_skin", "model_skel", texlist, texsyms));
                    scenenodes.Add(Skeleton("model_skel", model.RootJoint, Matrix4.Identity));
                    if (model.Animations != null)
                    {
                        animation = Animation("model_anim", "model_skel", model.Animations, startframe, numframes, framerate);
                    }
                }
                else
                {
                    scenenodes.Add(GeometryInstance("model", "model_mesh", texlist, texsyms, true));
                }
            }
            else
            {
                List<XNode> instnodes = new List<XNode>();
                int modelnum = 0;
                foreach (KeyValuePair<string, Texture> texture_kvp in model.Textures)
                {
                    string texname = texture_kvp.Key;
                    Texture texture = texture_kvp.Value;
                    List<Triangle> trilist = model.Triangles.Where(t => t.Texture == texture).ToList();
                    if (trilist.Count != 0)
                    {
                        bool visible = exclude_texture_prefixes.Count(x => texture.Name.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)) == 0;
                        string geometryname = String.Format("model{0}_mesh", modelnum);
                        Dictionary<string, Texture> texlist = new Dictionary<string, Texture> { { texname, texture } };
                        List<Vertex> vtxlist;
                        List<string> texsyms;
                        geometries.Add(Geometry(geometryname, trilist, texlist, out vtxlist, out texsyms));
                        libnodes.Add(GeometryInstance(geometryname + "_node", geometryname, texlist, texsyms, visible));
                        instnodes.Add(new XElement(ns + "instance_node", new XAttribute("url", "#" + geometryname + "_node")));
                        modelnum++;
                    }
                }

                libnodes.Add(
                    new XElement(ns + "node",
                        new XAttribute("id", "model_root"),
                        instnodes
                    )
                );

                scenenodes.Add(
                    new XElement(ns + "node",
                        new XAttribute("id", "model"),
                        new XElement(ns + "instance_node",
                            new XAttribute("url", "#model_root")
                        )
                    )
                );
            }

            return new XDocument(
                new XElement(ns + "COLLADA",
                    new XAttribute("version", "1.4.1"),
                    new XElement(ns + "asset",
                        new XElement(ns + "contributor",
                            new XElement(ns + "author", "IonFx")
                        ),
                        new XElement(ns + "created", model.CreatTime.ToString("O")),
                        new XElement(ns + "modified", model.ModTime.ToString("O")),
                        new XElement(ns + "unit",
                            new XAttribute("meter", "0.01"),
                            new XAttribute("name", "centimeter")
                        ),
                        new XElement(ns + "up_axis", "Z_UP")
                    ),
                    new XElement(ns + "library_images", model.Textures.Select(kvp => Image(kvp.Key, kvp.Value, model.Name))),
                    new XElement(ns + "library_effects", model.Textures.Select(kvp => Effect(kvp.Key))),
                    new XElement(ns + "library_materials", model.Textures.Select(kvp => Material(kvp.Key))),
                    new XElement(ns + "library_geometries", geometries),
                    controllers.Count == 0 ? null : new XElement(ns + "library_controllers", controllers),
                    animation == null ? null : new XElement(ns + "library_animations", animation),
                    libnodes.Count == 0 ? null : new XElement(ns + "library_nodes", libnodes),
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
    }
}

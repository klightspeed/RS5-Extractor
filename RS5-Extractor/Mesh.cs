using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RS5_Extractor
{
    public class Mesh
    {
        public Dictionary<string, Texture> Textures { get; set; }
        public List<Vertex> Vertices { get; set; }
        public List<Triangle> Triangles { get; set; }

        public Mesh()
        {
            Textures = new Dictionary<string,Texture>();
            Vertices = new List<Vertex>();
            Triangles = new List<Triangle>();
        }

        public Mesh(Mesh mesh)
        {
            Textures = new Dictionary<string,Texture>(mesh.Textures);
            Vertices = new List<Vertex>(mesh.Vertices);
            Triangles = new List<Triangle>(mesh.Triangles);
        }

        public Mesh Clone()
        {
            return new Mesh(this);
        }
    }
}

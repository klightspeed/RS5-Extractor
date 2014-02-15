These objects represent Immobile Models. Each model contains a Texture list `BHDR`, a Vertex list `VTXL` and a Triangle list `TRIL`. Each model also contains a `HEAD` structure, a Tag list `TAGL`, a `BBH ` list, and a `COLR` list. The name of the model typically has a `.h` extension for high LOD, `.m` extension for medium LOD, `.l` extension for a low LOD, or a `.x` extension for an extra-low LOD.

## `HEAD`

I'm not sure what the information in this chunk represents. This chunk is 52 bytes long.

Offset   | Size     | Type         | Meaning
---------|----------|--------------|----------
0x0000   | 4        | `int`        | Unknown - always seems to be 140.
0x0004   | 28       | `float[]`    | Unknown
0x0038   | 4        | `float`      | Appears to be the minimum LOD distance at High quality
0x003c   | 4        | `float`      | Appears to be the maximum LOD distance at High quality
0x0040   | 4        | `float`      | Appears to be the minimum LOD distance at Low quality
0x0044   | 4        | `float`      | Appears to be the maximum LOD distance at Low quality
0x0048   | 4        | `int`        | Unknown - might be flags.

## `BHDR`

This chunk contains the list of textures and sub-meshes used by the model. Each entry is 144 bytes long.

Offset   | Size     | Type         | Meaning
---------|----------|--------------|----------
0x0000   | 128      | `ASCIZ`      | Name of the object containing the texture
0x0080   | 4        | `int`        | Index of the first vertex in the sub-mesh
0x0084   | 4        | `int`        | Index of the first triangle vertex index in the sub-mesh
0x0088   | 4        | `int`        | Number of vertices in the sub-mesh
0x008c   | 4        | `int`        | Number of triangle vertex indices in the sub-mesh

## `VTXL`

This chunk contains the coordinates and normals of the vertices in the mesh. Each entry is 36 bytes long.  Note that it appears that the model is reflected.

Offset   | Size     | Type         | Meaning
---------|----------|--------------|----------
0x0000   | 4        | `float`      | X coordinate of vertex
0x0004   | 4        | `float`      | Y coordinate of vertex
0x0008   | 4        | `float`      | Z coordinate of vertex
0x000c   | 4        | `byte[]`     | Appears to be the normal unit vector of the vertex in {Z, Y, X} order and biased unsigned byte format, where 0x01 is -1.0, 0x80 is 0.0, and 0xFF is 1.0, 
0x0010   | 4        | `byte[]`     | Appears to be a unit vector in {Z, Y, X} order and biased unsigned byte format, where 0x01 is -1.0, 0x80 is 0.0, and 0xFF is 1.0; may be the tangent unit vector
0x0014   | 4        | `byte[]`     | Appears to be a unit vector in {Z, Y, X} order and biased unsigned byte format, where 0x01 is -1.0, 0x80 is 0.0, and 0xFF is 1.0; may be the binormal unit vector
0x0018   | 4        | `float`      | S texture coordinate of vertex
0x001c   | 4        | `float`      | Inverted T texture coordinate of vertex
0x0020   | 4        | ???          | Appears to be some sort of pointer, with 40 bytes between entries.

## `TRIL`

This chunk contains the indexes of the vertices of the triangles in the mesh. These indices are relative to the starting vertex index of the containing sub-mesh in the `BHDR` chunk. Each triangle takes 3 entries, each a 4 byte integer index.

## `TAGL`

This chunk appears to contain a list of attachment points. Each entry is 84 bytes long.

Offset   | Size     | Type         | Meaning
---------|----------|--------------|----------
0x0000   | 48       | `ASCIZ`      | Name of point
0x0030   | 12       | `float[]`    | Appears to be the coordinates of the attachment point.
0x003c   | 12       | `float[]`    | Appears to be the forward unit vector
0x0048   | 12       | `float[]`    | Appears to be the up unit vector

## `BBH `

I'm not sure what this chunk is, but it consists of a variable number of 4 byte integers.

## `COLR`

I'm not sure what this chunk is, but it consists of a variable number of 4 byte floating-point values.
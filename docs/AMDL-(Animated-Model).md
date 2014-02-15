# `AMDL` (Animated Model)

These objects represent Animated Models. Each model contains a `BLKS` Texture list, a `VTXS` Vertex list and a `INDS` Triangle list. Each model can also contain a `FRMS` list and a `JNTS` Joint list.

## `FRMS`

This chunk contains transformation matrices for the animations.  Each entry contains one 64-byte 4x4 transformation matrix (stored in top-to-bottom left-to-right order) for each joint in the model, with each value in the transformation matrix being a 4-byte `float`.

The individual animation start and end frames and the framerates are stored in the environment object in the environment.rs5 file.

## `JNTS`

This chunk contains the name, transformation matrix, and the index of the joint's parent joint. Each entry is 196 bytes long.

Offset   | Size     | Type         | Meaning
---------|----------|--------------|--------------
0x0000   | 128      | `ASCIZ`      | Null-terminated name of the joint
0x0080   | 64       | `float[,]`   | Reverse binding matrix of the joint.  Stored in top-to-bottom left-to-right order.
0x00c0   | 4        | `int`        | Index of the parent joint of this joint. Set to `-1` if this joint is the root joint.

## `BLKS`

This chunk contains the list of textures used by the model, and the range of vertices and triangle vertices these textures apply to. Each entry is 144 bytes long.

Offset   | Size     | Type         | Meaning
---------|----------|--------------|--------------
0x0000   | 128      | `ASCIZ`      | Name of the object containing the texture
0x0080   | 4        | `int`        | Index of first vertex the texture applies to
0x0084   | 4        | `int`        | Index after the last vertex the texture applies to
0x0088   | 4        | `int`        | Index of first triangle vertex the texture applies to
0x008c   | 4        | `int`        | Index after the last triangle vertex the texture applies to

## `VTXS`

This chunk contains the coordinates and normals of the vertices in the mesh.  Each entry is 32 bytes long.

Offset   | Size     | Type         | Meaning
---------|----------|--------------|--------------
0x0000   | 4        | `float`      | X coordinate of vertex
0x0004   | 4        | `float`      | Y coordinate of vertex
0x0008   | 4        | `float`      | Z coordinate of vertex
0x000c   | 4        | `float`      | S texture coordinate of vertex
0x0010   | 4        | `float`      | inverted T texture coordinate of vertex
0x0014   | 4        | `byte[]`     | Appears to be the normal of the vertex in {Z, Y, X} order and biased unsigned byte format, where 0x01 is -1.0, 0x80 is 0.0, and 0xFF is 1.0
0x0018   | 4        | `byte[]`     | Indices of joints affecting this vertex
0x001c   | 4        | `byte[]`     | Influences of above joints, where 0x00 is no influence and 0xFF is 100% influence.  These influences should add to 0xFF.

## `INDS`

This chunk contains the indexes of the vertices of the triangles in the mesh. These indices are absolute indices into the vertices in the `VTXS` chunk. Each triangle takes 3 entries, each a 4 byte integer index.

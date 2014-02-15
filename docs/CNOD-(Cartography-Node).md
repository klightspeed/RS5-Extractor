These objects appear to contain terrain data at various levels of detail.

The names of these objects are of the form `cterrllxxxxyyyy` where 
* `cterr` is the literal `cterr`
* `ll` is the level of detail (power-of-two units on a side) of the terrain block
* `xxxx` is the x coordinate offset of the terrain block
* `yyyy` is the y coordinate offset of the terrain block

Each node contains a `HEAD` chunk, a `VERT` vertex list, a `DDIF` chunk, a `DNRM` chunk, a `MGRP` chunk, a `MVTX` chunk and a `MIND` chunk.

## `HEAD`

Offset   | Size     | Type         | Meaning
---------|----------|--------------|--------------
0x0000   | 4        | `int`        | ???
0x0004   | 4        | `float`      | ???
0x0008   | 4        | `float`      | ???
0x000C   | 16       | ???          | ???
0x0010   | 4        | ???          | ???
0x0014   | 4        | `float`      | ???

## `VERT`

This is a 33x33 grid of 4-byte items. Each item appears to be 4 signed 8-bit values.

## `DDIF`

This appears to be a 64x64 grid of 8-byte items.  I don't know what the data represents.

## `DNRM`

This appears to be a 128x128 grid of bytes, with each byte representing the 4-bit x and y components of the normal.  0x88 would be a vertical normal.

## `MGRP`

## `MVTX`

This is a variable length array of 6-byte items.  It is only present in the highest LOD (6).

Offset   | Size     | Type         | Meaning
---------|----------|--------------|--------------
0x0000   | 2        | `short`      | Appears to be an index, possibly into the VERT array
0x0002   | 1        | `byte`       | Unknown - this and the 3 below add to 0xFF, 0xFE, 0xFD or 0xFC for 1, 2, 3 or 4 non-zero values respectively
0x0003   | 1        | `byte`       | ???
0x0004   | 1        | `byte`       | ???
0x0005   | 1        | `byte`       | ???

## `MIND`

This appears to be an array of 2048 triangles (two for each square on a 32x32 grid), each consisting of 3 `short` indexes into the `MVTX` array.  It is only present in the highest LOD (6).
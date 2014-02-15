# `INOD` (Instance Node)

These objects appear to contain position and rotation data. Each contains one 4 byte integer with the number of entries, followed by that number of 40 byte entries.

Offset   | Size     | Type         | Meaning
---------|----------|--------------|----------
0x0000   | 4        | `int`        | Appears to be a unique id.
0x0004   | 4        | `int`        | Appears to be an index into the model set list in `inst_header`.
0x0008   | 12       | `float`      | Appears to be position data.
0x0014   | 4        | ???          | Unknown.
0x0018   | 4        | `float`      | Appears to be a rotation around the Z axis in radians.
0x001c   | 4        | ???          | Unknown.
0x0020   | 8        | `float[]`    | Appears to be X and Y rotations in radians.

# `INOD` (Instance Node)

These objects appear to contain position and rotation data. Each contains one 4 byte integer with the number of entries, followed by that number of 40 byte entries.

Offset   | Size     | Type         | Meaning
---------|----------|--------------|----------
0x0000   | 4        | `int`        | Appears to be a unique id.
0x0004   | 4        | `int`        | Index into the model set list in `inst_header`.
0x0008   | 12       | `float`      | Position X, Y & Z coordinates. X and Y coordinates are on a scale of 0.0 to 8192.0 and are rotated 90 degrees counter-clockwise compared to the in-game map, so 0x0 is the bottom-left corner, 8192x0 is the top-left corner and 8192x8192 is the top-right corner.
0x0014   | 4        | ???          | Unknown.
0x0018   | 4        | `float`      | Appears to be a rotation around the Z axis in radians.
0x001c   | 4        | ???          | Unknown.
0x0020   | 8        | `float[]`    | Appears to be X and Y rotations in radians.

These objects contain the Level of Detail model sets. Each object contains a `DATA` chunk containing the list of models and presumably LOD distances.

## `DATA`

This chunk contains the list of LOD model parameters. Each entry is 136 bytes long.

Offset   | Size     | Type         | Meaning
---------|----------|--------------|----------
0x0000   | 128      | `ASCIZ`      | Model name
0x0080   | 4        | `float`      | Appears to be the approaching switch-to distance
0x0084   | 4        | `float`      | Appears to be the departing switch-to distance
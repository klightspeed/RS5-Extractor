# `IMAG` (Image)

This object represents a texture. It contains a `HEAD` chunk containing metadata, and a `DATA` chunk containing the DDS texture.

## `HEAD`

I'm not sure what this chunk contains apart from an ASCIZ name. It appears to always be 260 bytes long. Coincidentally, 260 bytes is the size of `MAX_PATH` under Windows.

## `DATA`

This chunk contains the DDS texture.

# IonFx Miasmata RS5 file format

The RS5 file format is a ZLib-compressed Tagged file format created by Joe Johnson of IonFx for the addictive survival game Miasmata.

Note that fixed-size fields containing variable-size data (such as the name fields in the central directory) will have garbage after the end of the variable-size data.

All data in the file is stored in little-endian order.

Types are expressed as DotNet types:
* `byte`: unsigned 8-bit integer
* `short`: signed 16-bit integer
* `int`: signed 32-bit integer
* `long`: signed 64-bit integer
* `float`: 32-bit floating-point
* `ASCIZ`: Null-terminated 8-bit ASCII string - note that the space after the string in a field is not zeroed, and may contain garbage.
* `FILETIME`: 64-bit Win32 FILETIME value
* `ARGB`: 8-bit Alpha, Red, Green and Blue values (32bpp)

## File Header

The file starts with a 24 byte header pointing to the central directory.

Offset   | Size     | Type         | Meaning
---------|----------|--------------|----------
0x0000   | 8        | `byte[]`     | Magic value `CFILEHDR`
0x0008   | 8        | `long`       | Absolute offset of central directory within file
0x0010   | 4        | `int`        | Size of each entry in central directory
0x0014   | 4        | `int`        | Unknown

## Central Directory

The central directory consists of one or more consecutive directory entries, each pointing to either nothing (offset = length = 0) or a tagged information object. The first entry is always a pointer to the central directory itself. Each directory entry appears to always be 168 bytes long.


Offset   | Size     | Type         | Meaning
---------|----------|--------------|----------
0x0000   | 8        | `long`       | Absolute offset of object within file
0x0008   | 4        | `int`        | Compressed length of object
0x000C   | 4        | ???          | Unknown - seems to always be `0x80000000` for present entries
0x0010   | 4        | ???          | Unknown - seems to always be `0x00000300` for present entries
0x0014   | 4        | `byte[]`     | FourCC - identifies type of object
0x0018   | 8        | long         | The size of the uncompressed data shifted left by one bit. The low bit is always 1 for a valid entry and 0 otherwise.
0x0020   | 8        | `FILETIME`   | Appears to be the modification time of the object
0x0028   | 128      | `ASCIZ`      | Null-terminated name of object

## Tagged information format

Each object is compressed using the RFC1950 Zlib compression format. Each object is wrapped in a tagged chunk and can contain either data or one or more tagged chunks. All chunks within the compressed objects are aligned to 8 bytes. The chunk size in the chunk header does not include this alignment padding.

Offset   | Size     | Type         | Meaning
---------|----------|--------------|----------
0x0000   | 4        | `byte[]`     | FourCC - identifies type of chunk
0x0004   | 2        | ???          | Unknown - always appears to be zero
0x0006   | 1        | `byte`       | Length of name including terminating zero
0x0007   | 1        | `byte`       | zero if the chunk contains data; 1 if the chunk either contains no data or contains tagged chunks.
0x0008   | 4        | `int`        | Size of the chunk
0x000c   | variable | `ASCIZ`      | Null-terminated name of chunk
align(8) | variable |              | Chunk data or tagged chunks

## Objects

Object types are identified by a few tags. Anything that doesn't fit in these tags (such as terrain height maps) is tagged as `RAW.`.

Object types:
* [`AMDL`](AMDL-\(Animated-Model\).md) Animated Model
* [`IMDL`](IMDL-\(Immobile-Model\).md) Immobile Model
* [`IMAG`](IMAG-\(Image\).md) Image
* [`IIMP`](IIMP-\(Image-based-Imposter\).md) Image-based Imposter
* [`MSET`](MSET-\(Model-Set\).md) Model Set
* [`SAMP`](SAMP-\(Sound-Sample\).md) Sound Sample
* [`PROF`](PROF-\(Shader-Profile\).md) Shader Profile
* [`CONF`](CONF-\(Shader-Configuration\).md) Shader Configuration
* [`INOD`](INOD-\(Instance-Node\).md) Instance Node
* [`CNOD`](CNOD-\(Cartography-Node\).md) Cartography Node
* [`FOGN`](FOGN-\(Fog-Noise-Map\).md) Fog Noise Map
* [`RAW.`](RAW.-\(Raw-Data\).md) Raw Data

Special objects:
* [`environment`](Environment-settings.md)

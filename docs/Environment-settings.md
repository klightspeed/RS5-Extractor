This file is encoded in a binary serialized format.  Each value begins with a type byte.  Following the type byte is the variable data.

The following types are known:
* `T` (0x54): Dictionary - pairs of null-terminated string keys and typed values.  Closed when a zero-length key is encountered.
* `I` (0x49): Integer array - 32-bit integer count followed by count 32-bit integer values.
* `i` (0x69): Integer - 32-bit integer.
* `F` (0x46): Float array - 32-bit integer count followed by count 32-bit (single-precision) floating-point values.
* `f` (0x66): Float - 32-bit (single-precision) floating-point value.
* `S` (0x53): String array - 32-bit integer count followed by count null-terminated strings.
* `s` (0x73): String - null-terminated string.
* `M` (0x4D): Typed array - 32-bit integer count followed by count typed values.
* `R` (0x52): _Reference(?)_ array - 32-bit integer count followed by count _null-terminated reference strings(?)_
* `.` (0x2E): Null
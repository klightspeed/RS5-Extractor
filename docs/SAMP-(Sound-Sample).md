These objects contain sound in an unknown codec. Each contains a `HEAD` chunk containing the sample rate and number of samples, and a `DATA` chunk containing the encoded audio data.

## `HEAD`

Offset   | Size     | Type         | Meaning
---------|----------|--------------|----------
0x0000   | 4        | `int`        | Sample rate (Hz)
0x0004   | 4        | `int`        | Number of samples (?)
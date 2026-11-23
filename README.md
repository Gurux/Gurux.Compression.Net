# Gurux.Compression.Net

Gurux.Compression.Net compresses and decompresses binary data using V.44.
Use it when exchanging data with a device or application that supports V.44.
Text can be compressed by encoding it into bytes first.

The library targets .NET 9, .NET 10 and .NET 11.

## Install

Add the package to your application when it is available from your configured NuGet feed:

```bash
dotnet add package Gurux.Compression.Net
```

## Compress and decompress

```csharp
using System.Text;
using Gurux.Compression;

byte[] original = Encoding.UTF8.GetBytes("Hello, hello, hello!");
byte[] compressed = new GXV44Encoder().Compress(original);
byte[] restored = new GXV44Decoder().Decompress(compressed);
string text = Encoding.UTF8.GetString(restored);
```

Both sides must use matching options. Configure them when needed:

```csharp
var options = new GXv44Options
{
    HistoryLimit = 3072,
    MaxCodewords = 1024
};
var encoder = new GXV44Encoder(options);
var decoder = new GXV44Decoder(options);
```

## Files and streams

```csharp
using var source = File.OpenRead("input.bin");
using var destination = File.Create("input.v44");
new GXV44Encoder().Compress(source, destination);
```

Use `GXV44Decoder.Decompress(source, destination)` to restore a compressed stream.
The component leaves both streams open. Use separate input and output streams.
Instances must be used by one operation at a time.

Optional `OnTrace` callbacks expose compressed data as a bit string such as
`"100011"`. Without a callback, no trace bit string is created.

## License

This software is available under either of the following licenses:

- **Open-source license:** GNU General Public License, version 2 only
  (GPL-2.0-only). See [LICENSE](LICENSE) for the full license text.
- **Commercial license:** Available from Gurux Ltd under a separate
  written agreement. This option allows use under the terms of that
  agreement instead of GPL-2.0-only.

A valid commercial license is required to use the commercial licensing
option. See [Gurux Commercial License Notice](LICENSES/LicenseRef-Gurux-Commercial.txt).

For commercial licensing inquiries, [contact Gurux Ltd](https://www.gurux.fi/).

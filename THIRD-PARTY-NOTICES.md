# Third-Party Notices

Light.PortableResults incorporates adapted primitive-formatting code from:

- Project: .NET Runtime
- Repository: https://github.com/dotnet/runtime
- Source line: `release/6.0`
- Immutable source: tag `v6.0.36`, commit `f1dd57165bfd91875761329ac3a8b17f6606ad18`
- Adapted files under `src/libraries/System.Private.CoreLib/src/System`:
  `Number.Grisu3.cs`, `Number.DiyFp.cs`, `Number.Dragon4.cs`, `Number.BigInteger.cs`,
  `Number.NumberBuffer.cs`, `Number.Formatting.cs`, `Decimal.DecCalc.cs`,
  `Globalization/DateTimeFormat.cs`, and `Guid.cs`
- Adapted XML file: `src/libraries/System.Private.Xml/src/System/Xml/Schema/XsdDuration.cs`
- Local files: the adapted implementation files under
  `src/Light.PortableResults/Numbers` and `src/Light.PortableResults/Text`, as detailed in those
  folders' `README.md` files

## .NET Foundation MIT License

The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

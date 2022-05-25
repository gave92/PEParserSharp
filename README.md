# PEParserSharp

![logo](https://img.shields.io/badge/license-BSD-blue.svg)&nbsp;[![donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.me/gave92)

PE (Portable Executable) files parser and icon extractor written in C#.
- Can extract resources and icons from dll, exe files.
- Fully written in C#.
- Does not use Win32 API/PInvoke

Based on [PeParser](https://github.com/dorkbox/PeParser) for Java.

## Installation

$ Install-Package Gave.Libs.PEParserSharp

## Quick guide

Parses and displays info about "imageres.dll". Loads 3 icons from the dll.

```cs
using PEParserSharp;
using System;
using System.Linq;
using IO = System.IO;

...

var imageresDll = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SystemResources", "imageres.dll.mun");
var pe = new PeFile(imageresDll);
Console.WriteLine(pe.Info);
var icons = pe.ExtractIcons(256, new[] { 3, 35, 109 }); // Folder, Disk, This PC
```

## Supported platforms

fbchat-sharp has been created as a PCL targeting .NET Standard 2.0 that supports a wide range of platforms. The list includes but is not limited to:
* .NetStandard 2.0
* .NET Core 2.0
* .NET Framework 4.6.1
* Universal Windows Platform

© Copyright 2022 by Marco Gavelli

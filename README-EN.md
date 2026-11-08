# Silver Pines Arabic Translation by P A M — installer source 0.6.4

## Build on Windows

1. Install .NET Framework 4.8 and Python 3 if needed.
2. Open PowerShell in this source directory and run:
   py -3 source/build_installer.py
3. The script invokes the .NET Framework C# compiler and creates SilverPinesArabic.exe in this directory.

The source archive contains the C# installer and its build script, translation text, and patch manifest.
The distribution ZIP provides the patch files and Amiri font required at runtime. The installer checks
the Steam game build and original file hashes and applies the verified resource patches locally.
It does not download components, extract an embedded installer, or launch another executable.

SHA256SUMS.txt identifies the previously built release ZIP and its EXE. Local rebuild hashes can
differ because this compiler does not guarantee byte-for-byte reproducibility.
The target is Silver Pines for Windows on Steam, build 26489514.
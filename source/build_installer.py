from pathlib import Path
import subprocess

root = Path(__file__).resolve().parent.parent
source = root / "source"
csc = Path(r"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe")
subprocess.run([
    str(csc), "/nologo", "/target:winexe", "/platform:anycpu", "/optimize+",
    "/reference:System.Windows.Forms.dll", "/reference:System.Drawing.dll",
    "/reference:System.Web.Extensions.dll", "/out:" + str(root / "SilverPinesArabic.exe"),
    str(source / "Installer.cs"), str(source / "InstallerForm.cs"),
], check=True)

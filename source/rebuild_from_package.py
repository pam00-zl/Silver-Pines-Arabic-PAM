"""Reconstruct resource files into a separate folder; never installs them.
python rebuild_from_package.py GAME_FOLDER OUTPUT_FOLDER
Requires verified original files or the installer's hash-named backups.
Supports SPARUI01/02 copy patches and SPARUI03/04 additive patches.
"""
from pathlib import Path
import sys,json,hashlib,struct,gzip

def reconstruct(old,patch):
 magic=patch[:8];assert magic in [b'SPARUI01',b'SPARUI02',b'SPARUI03',b'SPARUI04']
 assert patch[8:40]==hashlib.sha256(old).digest()
 length=struct.unpack_from('<Q',patch,72)[0]
 raw=memoryview(gzip.decompress(patch[80:])if magic in [b'SPARUI01',b'SPARUI03']else patch[80:])
 result=bytearray();p=0
 if magic in [b'SPARUI01',b'SPARUI02']:
  while raw[p]!=255:
   op=raw[p];p+=1
   if op==0:
    off,n=struct.unpack_from('<QI',raw,p);p+=12;assert off+n<=len(old);result+=old[off:off+n]
   elif op==1:
    n=struct.unpack_from('<I',raw,p)[0];p+=4;result+=raw[p:p+n];p+=n
   else:raise ValueError('Invalid patch opcode')
   assert len(result)<=length
 else:
  count=struct.unpack_from('<I',raw)[0];p=4;oldpos=0
  for _ in range(count):
   x,y,z=struct.unpack_from('<IIq',raw,p);p+=16;assert p+x+y<=len(raw)
   result+=bytes((raw[p+i]+(old[oldpos+i]if 0<=oldpos+i<len(old)else 0))&255 for i in range(x));p+=x
   result+=raw[p:p+y];p+=y;oldpos+=x+z;assert len(result)<=length
 assert len(result)==length and hashlib.sha256(result).digest()==patch[40:72]
 return bytes(result)

def local(path):return Path('\\\\?\\'+str(path))if sys.platform=='win32'else path

if __name__=='__main__':
 package=Path(__file__).resolve().parent.parent
 game=Path(sys.argv[1]).resolve();out=Path(sys.argv[2]).resolve()
 assert out!=game and game not in out.parents and out not in game.parents,'Use a separate output directory'
 manifest=json.loads((package/'manifest.json').read_text(encoding='utf-8'))
 H=lambda b:hashlib.sha256(b).hexdigest()
 for f in manifest['files']:
  old=local(game/f['path']).read_bytes()
  if H(old)!=f['original_sha256']:old=local(game/'_SilverPinesArabic_UI'/(f['original_sha256']+'.bin')).read_bytes()
  assert H(old)==f['original_sha256']
  patch=(package/f['patch']).read_bytes();assert H(patch)==f['patch_sha256']
  data=reconstruct(old,patch);assert H(data)==f['patched_sha256']
  target=local(out/f['path']);target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
 print('Reconstructed',len(manifest['files']),'verified resource files into',out)

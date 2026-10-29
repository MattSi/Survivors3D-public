"""Import a Unity package with its original GUIDs; never overwrite differing local files."""
from pathlib import Path, PurePosixPath
import tarfile, json

ROOT=Path(__file__).resolve().parents[2]
PACKAGE=Path('C:/temp/20260907/kubikos_3d_cube_world.unitypackage')
with tarfile.open(PACKAGE,'r:gz') as archive:
    members={m.name:m for m in archive.getmembers()}
    entries={}
    for name,member in members.items():
        if not name.endswith('/pathname'):continue
        path=archive.extractfile(member).read().decode('utf-8-sig').strip()
        relative=PurePosixPath(path)
        if relative.is_absolute() or '..' in relative.parts or relative.parts[0]!='Assets':
            raise ValueError('Invalid package path: '+path)
        target=(ROOT/path).resolve()
        if not target.is_relative_to((ROOT/'Assets').resolve()):raise ValueError(path)
        entries[name.split('/')[0]]=target
    # Preflight all destinations before writing any assets.
    for guid,target in entries.items():
        for suffix,dest in [('asset',target),('asset.meta',Path(str(target)+'.meta'))]:
            member=members.get(guid+'/'+suffix)
            if member is None:continue
            if dest.is_file() and dest.read_bytes()!=archive.extractfile(member).read():
                raise RuntimeError('Existing different asset: '+str(dest))
    count=0
    for guid,target in entries.items():
        asset=members.get(guid+'/asset')
        if asset is None:target.mkdir(parents=True,exist_ok=True)
        for suffix,dest in [('asset',target),('asset.meta',Path(str(target)+'.meta'))]:
            member=members.get(guid+'/'+suffix)
            if member is None:continue
            dest.parent.mkdir(parents=True,exist_ok=True)
            dest.write_bytes(archive.extractfile(member).read())
        count+=1
    print('IMPORTED',count,'assets with original GUIDs')

from pathlib import Path
import re, json, uuid, math, random
ROOT=Path(__file__).resolve().parents[2]
bounds=json.loads((ROOT/'ArtSource/World/model-bounds.json').read_text())
inventory=json.loads((ROOT/'Logs/scene-package-inventory.json').read_text())
byguid={x['guid']:x['path'] for x in inventory}
def meta(path,folder=False):
 p=ROOT/(path+'.meta')
 if p.exists():return re.search(r'guid: (\w+)',p.read_text()).group(1)
 g=uuid.uuid4().hex;p.parent.mkdir(parents=True,exist_ok=True)
 p.write_text(f'fileFormatVersion: 2\nguid: {g}\n'+('folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n' if folder else ''))
 return g
for folder in ['Assets/Materials/ForestArena']: (ROOT/folder).mkdir(exist_ok=True);meta(folder,True)
source=(ROOT/'Assets/Scenes/Playground.unity').read_text(encoding='utf-8')
docs=re.split(r'(?=^--- !u!)',source,flags=re.M)
def docid(d):
 m=re.match(r'--- !u!\d+ &(\d+)',d);return int(m[1]) if m else -1
floor=''.join(d for d in docs if 1200<=docid(d)<=1203)
scene=[d for d in docs if not 1200<=docid(d)<2000 and 'SceneRoots:' not in d]
roots=[330585546,410087041,1001,1101]
records=[];nextid=900100;children=[];materialmap={};materialinfo={}

def flatmat(name,color):
 path='Assets/Materials/ForestArena/'+name+'.mat';guid=meta(path)
 text=(ROOT/'Assets/Materials/Arena Floor.mat').read_text()
 text=re.sub(r'm_Name: .*',f'm_Name: {name}',text)
 r,g,b=color
 text=re.sub(r'(_BaseColor|_Color): \{[^}]+\}',lambda m:m[1]+f': {{r: {r}, g: {g}, b: {b}, a: 1}}',text)
 (ROOT/path).write_text(text)
 materialinfo[guid]={'color':color}
 return guid
grass=flatmat('Clearing Grass',(.23,.34,.12));dirt=flatmat('Camp Path',(.38,.27,.14));stone=flatmat('Boundary Stone',(.28,.31,.24))

def mapped_material(guid):
 if guid in materialmap:return materialmap[guid]
 original=ROOT/byguid[guid];old=original.read_text(encoding='utf-8-sig')
 # Build clean Lit materials; legacy shader properties can carry incompatible render state.
 text=(ROOT/'Assets/Materials/Arena Floor.mat').read_text()
 text=re.sub(r'm_Name: .*',f'm_Name: {original.stem}',text)
 text=re.sub(r'(_BaseColor|_Color): \{[^}]+\}',lambda m:m[1]+': {r: 1, g: 1, b: 1, a: 1}',text)
 tex=None
 for key in ['_Albedo','_BaseMap','_MainTex']:
  tex=re.search(r'- '+key+r':\s+m_Texture: \{fileID: \d+, guid: (\w+)',old)
  if tex:break
 if tex:
  text=re.sub(r'(- _BaseMap:\s+m_Texture: )\{[^}]+\}',lambda m:m[1]+'{fileID: 2800000, guid: '+tex[1]+', type: 3}',text)
 text=re.sub(r'- _Smoothness: .*','- _Smoothness: 0.05',text)
 path='Assets/Materials/ForestArena/'+original.stem+'.mat';newguid=meta(path)
 (ROOT/path).write_text(text)
 materialmap[guid]=newguid
 materialinfo[newguid]={'texture':byguid[tex[1]]} if tex else {'color':[1,1,1]}
 return newguid

def emit(text,name,pos,scale,yaw):
 global nextid
 ids=re.findall(r'^--- !u!\d+ &(\d+)',text,re.M);mapping={old:str(nextid+i) for i,old in enumerate(ids)};nextid+=len(ids)+1
 text=re.sub(r'^(--- !u!\d+ &)(\d+)',lambda m:m[1]+mapping[m[2]],text,flags=re.M)
 text=re.sub(r'\{fileID: (\d+)\}',lambda m:'{fileID: '+mapping.get(m[1],m[1])+'}',text)
 text=re.sub(r'^%[^\n]*\n','',text,flags=re.M)
 parts=re.split(r'(?=^--- !u!)',text,flags=re.M)
 root=next(d for d in parts if d.startswith('--- !u!4 ') and 'm_Father: {fileID: 0}' in d)
 tid=re.search(r'--- !u!4 &(\d+)',root)[1]
 gid=re.search(r'm_GameObject: {fileID: (\d+)}',root)[1]
 angle=math.radians(yaw)/2
 changed=re.sub(r'm_LocalPosition: \{[^}]*\}',f'm_LocalPosition: {{x: {pos[0]}, y: {pos[1]}, z: {pos[2]}}}',root)
 changed=re.sub(r'm_LocalScale: \{[^}]*\}',f'm_LocalScale: {{x: {scale[0]}, y: {scale[1]}, z: {scale[2]}}}',changed)
 changed=re.sub(r'm_LocalRotation: \{[^}]*\}',f'm_LocalRotation: {{x: 0, y: {math.sin(angle)}, z: 0, w: {math.cos(angle)}}}',changed)
 changed=changed.replace('m_Father: {fileID: 0}','m_Father: {fileID: 900001}')
 parts[parts.index(root)]=changed
 for i,d in enumerate(parts):
  if d.startswith('--- !u!1 &'+gid+'\n'):parts[i]=re.sub(r'm_Name: .*',f'm_Name: {name}',d,count=1)
 text=''.join(parts)
 text=re.sub(r'm_StaticEditorFlags: \d+','m_StaticEditorFlags: 0',text)
 children.append(int(tid));scene.append(text)

def cube(name,pos,scale,mat):
 text=re.sub(r'fileID: 2100000, guid: \w+',f'fileID: 2100000, guid: {mat}',floor)
 emit(text,name,pos,scale,0)
 records.append({'cube':True,'name':name,'pos':pos,'scale':scale,'material':mat})

def prop(short,x,z,height,yaw=0,y=0):
 info=bounds[short];factor=height/info['size'][2]
 pos=[x,y-info['min'][2]*factor,z];scale=[factor]*3
 text=(ROOT/info['prefab']).read_text(encoding='utf-8-sig')
 text=re.sub(r'(fileID: 2100000, guid: )(\w+)',lambda m:m[1]+mapped_material(m[2]),text)
 emit(text,short.split('/')[-1],pos,scale,yaw)
 records.append({'prefab':short,'pos':pos,'scale':scale,'yaw':yaw,'materials':[mapped_material(re.search(r'guid: (\w+)',(ROOT/(p+'.meta')).read_text())[1]) for p in info['materials']]})

# Broad raised grass island. All gameplay remains on the existing 36 x 36 flat arena.
for ix in range(-5,5):
 for iz in range(-5,5):
  if abs(ix+.5)+abs(iz+.5)>8:continue
  short='Cubes/Cube_SoilWGrass_1';info=bounds[short]
  text=(ROOT/info['prefab']).read_text(encoding='utf-8-sig')
  text=re.sub(r'(fileID: 2100000, guid: )(\w+)',lambda m:m[1]+mapped_material(m[2]),text)
  pos=[ix*6+3,-info['max'][2],iz*6+3];scale=[6,1,6]
  emit(text,'Grass Island Tile',pos,scale,0)
  records.append({'prefab':short,'pos':pos,'scale':scale,'yaw':0,'materials':[mapped_material(re.search(r'guid: (\w+)',(ROOT/(p+'.meta')).read_text())[1]) for p in info['materials']]})
cube('Quiet Central Clearing',[0,.003,0],[35.9,.008,35.9],grass)
cube('North South Trail',[0,.009,0],[3.6,.008,36],dirt)
cube('East West Trail',[0,.010,0],[36,.008,3.2],dirt)
for sign in [-1,1]:
 cube('Arena Stone Edge',[sign*18.15,.09,0],[.3,.18,36.6],stone)
 cube('Arena Stone Edge',[0,.09,sign*18.15],[36.6,.18,.3],stone)
random.seed(20260907)
for side in range(4):
 for along in [-25,-20,-15,-10,-5,0,5,10,15,20,25]:
  edge=random.uniform(22.5,25.5)
  x,z=(along,edge) if side==0 else (along,-edge) if side==1 else (edge,along) if side==2 else (-edge,along)
  if side==0 and -7<along<7:continue
  prop('TreesAndPlants/Tree_'+str(random.randint(1,3)),x,z,random.uniform(3.5,5.2),random.randrange(360))
for side in range(4):
 for along in [-15,-10,-5,5,10,15]:
  x,z=(along,18.8) if side==0 else (along,-18.8) if side==1 else (18.8,along) if side==2 else (-18.8,along)
  prop('Items/FanceWooden',x,z,.85,0 if side<2 else 90)
for x,z in [(-20,-19),(-21,9),(20,-12),(21,17),(-18,22),(13,22),(-25,-8),(25,7)]:
 prop('Rocks/Green/Rock_3',x,z,random.uniform(1.1,2.0),random.randrange(360))
 prop('TreesAndPlants/MushroomRed',x+1,z+.7,.4,random.randrange(360))
for x,z in [(-8,21),(8,21),(-22,-3),(21,-3)]:prop('TreesAndPlants/Stump1',x,z,.6,25)
prop('Items/Camp',-4,23,2.7,180)
prop('Items/Camp',4,23,2.4,180)
prop('Items/FirePlace',0,21,0.6,0)
prop('Items/Chest',-7,21,.75,0)
for x,z in [(7,22),(8,23),(-7,24)]:prop('Items/Box',x,z,.85,random.randrange(30))
for x,z in [(6,21),(-6,23)]:prop('Items/Barrel',x,z,.85,0)
for i in range(60):
 side=i%4;along=random.uniform(-26,26);edge=random.uniform(19.5,27)
 x,z=(along,edge) if side==0 else (along,-edge) if side==1 else (edge,along) if side==2 else (-edge,along)
 prop('TreesAndPlants/Flower1' if i%4==0 else 'TreesAndPlants/Grass3',x,z,random.uniform(.25,.55),random.randrange(360))

group=f'''--- !u!1 &900000
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 900001}}
  m_Layer: 0
  m_Name: Forest Camp Arena
  m_TagString: Untagged
  m_IsActive: 1
--- !u!4 &900001
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 900000}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_Children:
'''+''.join(f'  - {{fileID: {i}}}\n' for i in children)+'''  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
'''
scene.append(group);roots.append(900001)
scene.append('--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n'+''.join(f'  - {{fileID: {i}}}\n' for i in roots))
text=''.join(scene).replace('m_Name: Playground\n','m_Name: Forest Camp Survival\n')
text=re.sub(r'm_BackGroundColor: \{[^}]+\}','m_BackGroundColor: {r: 0.11, g: 0.17, b: 0.20, a: 1}',text)
path='Assets/Scenes/ForestCamp.unity';(ROOT/path).write_text(text,encoding='utf-8');guid=meta(path)
settings=ROOT/'ProjectSettings/EditorBuildSettings.asset';text=settings.read_text()
if path not in text:text=text.replace('  m_Scenes:\n',f'  m_Scenes:\n  - enabled: 1\n    path: {path}\n    guid: {guid}\n')
settings.write_text(text)
(ROOT/'ArtSource/World/layout.json').write_text(json.dumps({'objects':records,'materials':materialinfo},indent=2))
print('FOREST_SCENE_CREATED',len(records),'objects',len(materialinfo),'URP materials')

import bpy, json, re
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
inventory=json.loads((ROOT/'Logs/scene-package-inventory.json').read_text())
byguid={x['guid']:x['path'] for x in inventory}
prefabs=['TreesAndPlants/Tree_1','TreesAndPlants/Tree_2','TreesAndPlants/Tree_3',
 'TreesAndPlants/Stump1','TreesAndPlants/Grass3','TreesAndPlants/Flower1','TreesAndPlants/MushroomRed',
 'Rocks/Green/Rock_3','Rocks/Green/RockFlat_2','Items/Camp','Items/Box','Items/Barrel',
 'Items/FanceWooden','Items/Chest','Items/FirePlace','Cubes/Cube_SoilWGrass_1']
results={}
for short in prefabs:
 path='Assets/KUBIKOS - World/URP Support/Prefabs URP/'+short+'.prefab'
 text=(ROOT/path).read_text(encoding='utf-8-sig')
 mesh=re.search(r'm_Mesh: \{fileID: (\d+), guid: (\w+)',text)
 mats=re.findall(r'fileID: 2100000, guid: (\w+)',text)
 bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
 bpy.ops.import_scene.fbx(filepath=str(ROOT/byguid[mesh[2]]))
 points=[o.matrix_world@Vector(v) for o in bpy.context.scene.objects if o.type=='MESH' for v in o.bound_box]
 lower=[min(v[i] for v in points) for i in range(3)]
 upper=[max(v[i] for v in points) for i in range(3)]
 results[short]={'prefab':path,'fbx':byguid[mesh[2]],'min':lower,'max':upper,'size':[upper[i]-lower[i] for i in range(3)],
 'materials':[byguid[g] for g in mats]}
(ROOT/'ArtSource/World/model-bounds.json').write_text(json.dumps(results,indent=2))
print(json.dumps({k:v['size'] for k,v in results.items()},indent=2))

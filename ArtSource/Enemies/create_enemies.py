import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Resources/Characters'
SOURCE=Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
palette={'Enemy_Stone':(.32,.38,.40),'Enemy_Dark':(.075,.10,.13),
         'Enemy_Bone':(.7,.68,.53),'Enemy_Eye':(.95,.27,.08), 'Enemy_Moss':(.24,.34,.28)}
mats={}
for name,c in palette.items():
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True
 m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*c,1)
 m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.9
 mats[name]=m
parts=[]
def box(name,pos,size,mat,bevel=.015):
 bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.name=name;o.dimensions=size
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 if bevel:
  mod=o.modifiers.new('Flat bevel','BEVEL');mod.width=bevel;mod.segments=1;bpy.ops.object.modifier_apply(modifier=mod.name)
 o.data.materials.append(mats[mat]);parts.append(o);return o
def export(name,shift):
 # Export one mesh with material slots to keep enemy renderer overhead low.
 bpy.ops.object.select_all(action='DESELECT')
 for part in parts:part.select_set(True)
 bpy.context.view_layer.objects.active=parts[0]
 bpy.ops.object.join()
 joined=bpy.context.object;joined.name=name
 parts.clear();parts.append(joined)
 bpy.ops.object.empty_add(location=(0,-.3,.6));anchor=bpy.context.object;anchor.name='FaceAnchor';parts.append(anchor)
 bpy.ops.object.select_all(action='DESELECT')
 for p in parts:p.select_set(True)
 bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},
  axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
 stats[name]=sum(sum(len(f.vertices)-2 for f in p.data.polygons) for p in parts if p.type=='MESH')
 for p in parts:p.location.x+=shift
 parts.clear()
stats={}

# A wide-eared, horned imp with a conspicuous brow.
for x in [-.16,.16]:box('ImpFoot',(x,0,.09),(.22,.28,.18),'Enemy_Dark')
box('ImpBody',(0,0,.33),(.43,.32,.35),'Enemy_Moss',.025)
box('ImpHead',(0,0,.65),(.65,.48,.44),'Enemy_Stone',.035)
for x in [-.4,.4]:box('ImpEar',(x,0,.7),(.2,.16,.17),'Enemy_Moss')
for x in [-.22,.22]:
 o=box('ImpHorn',(x,.03,.935),(.12,.14,.18),'Enemy_Bone');o.rotation_euler.y=math.copysign(.25,x)
for x in [-.15,.15]:
 box('ImpEye',(x,-.25,.72),(.115,.025,.085),'Enemy_Eye',.007)
 box('ImpBrow',(x,-.262,.79),(.16,.035,.038),'Enemy_Dark',.004)
box('ImpMouth',(0,-.251,.555),(.22,.03,.06),'Enemy_Dark',.004)
export('EnemyImp',-1.35)

# A hooded crawler: a dark silhouette with a bright inset face.
box('HoodBody',(0,0,.35),(.53,.37,.57),'Enemy_Dark',.035)
box('HoodCrown',(0,.04,.79),(.67,.48,.22),'Enemy_Stone',.025)
for x in [-.275,.275]:box('HoodSide',(x,0,.61),(.15,.48,.39),'Enemy_Stone')
box('HoodFace',(0,-.20,.65),(.42,.035,.24),'Enemy_Dark',.008)
for x in [-.12,.12]:box('HoodEye',(x,-.224,.70),(.1,.025,.066),'Enemy_Eye',.006)
for x in [-.19,.19]:box('HoodFoot',(x,-.035,.08),(.19,.28,.16),'Enemy_Dark')
box('HoodBelt',(0,-.01,.32),(.56,.4,.065),'Enemy_Bone',.008)
box('HoodPendant',(0,-.225,.43),(.10,.05,.12),'Enemy_Bone',.009)
export('EnemyHood',0)

# A chunky rockling with broad arms and asymmetrical stone plates.
for x in [-.18,.18]:box('RockFoot',(x,0,.11),(.27,.35,.22),'Enemy_Dark',.025)
box('RockTorso',(0,0,.40),(.61,.43,.40),'Enemy_Stone',.035)
box('RockHead',(0,0,.75),(.71,.49,.33),'Enemy_Stone',.045)
for x in [-.41,.41]:box('RockArm',(x,0,.44),(.18,.34,.4),'Enemy_Moss',.025)
box('RockCrown',(-.17,.015,.955),(.30,.40,.12),'Enemy_Moss')
for x in [-.15,.15]:box('RockEye',(x,-.254,.80),(.105,.025,.065),'Enemy_Eye',.008)
box('RockMouth',(0,-.255,.67),(.22,.026,.043),'Enemy_Dark',.003)
box('RockCrack',(.08,-.225,.43),(.035,.018,.22),'Enemy_Dark',.001)
export('EnemyRock',1.35)

# Render the three models together at a gameplay-like camera elevation.
bpy.ops.mesh.primitive_plane_add(size=200)
floor=bpy.context.object;floor.name='Preview Floor'
mat=bpy.data.materials.new('Floor');mat.diffuse_color=(.08,.11,.13,1);floor.data.materials.append(mat)
for loc,power,size in [((2,-4,6),500,5),((-4,-2,3),250,4),((0,4,5),450,4)]:
 bpy.ops.object.light_add(type='AREA',location=loc);o=bpy.context.object;o.data.energy=power;o.data.size=size
 o.rotation_euler=(Vector((0,0,.5))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(1,-5,7.7));cam=bpy.context.object
cam.rotation_euler=(Vector((0,0,.5))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=4.5
scene=bpy.context.scene;scene.camera=cam;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1400;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.world.color=(.15,.15,.15);scene.view_settings.view_transform='AgX'
scene.render.filepath=str(SOURCE/'Enemies-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'BlockEnemies.blend'))
(SOURCE/'model-info.json').write_text(json.dumps(stats,indent=2))
bpy.ops.render.render(write_still=True)
print('ENEMIES_EXPORT_COMPLETE')

"""Forest Stone King: editable block model with independent shoulder pivots and runtime glow slots."""
import bpy
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
SOURCE=Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
colors={'Boss_Stone':(.30,.34,.31),'Boss_Slab':(.39,.42,.37),'Boss_Moss':(.35,.42,.19),
        'Boss_Wood':(.32,.21,.14),'Boss_Dark':(.16,.19,.20),'Boss_Core':(1,.49,.06),'Boss_Seam':(.6,.30,.045)}
mats={}
for name,color in colors.items():
    mat=bpy.data.materials.new(name); mat.diffuse_color=(*color,1); mat.use_nodes=True
    node=mat.node_tree.nodes['Principled BSDF']; node.inputs['Base Color'].default_value=(*color,1)
    node.inputs['Roughness'].default_value=.87
    if name=='Boss_Core':
        node.inputs['Emission Color'].default_value=(1,.4,.03,1); node.inputs['Emission Strength'].default_value=.65
    mats[name]=mat
parts=[]
def box(name,pos,size,material,bevel=.025,parent=None):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos)
    obj=bpy.context.object; obj.name=name; obj.dimensions=size
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    mod=obj.modifiers.new('Flat bevel','BEVEL'); mod.width=bevel; mod.segments=1
    bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.materials.append(mats[material])
    if parent:
        matrix=obj.matrix_world.copy(); obj.parent=parent; obj.matrix_world=matrix
    parts.append(obj); return obj
def pivot(name,pos):
    bpy.ops.object.empty_add(location=pos); obj=bpy.context.object; obj.name=name; parts.append(obj); return obj

for side in [-1,1]:
    box('KingFoot',(side*.39,-.06,.20),(.64,.84,.40),'Boss_Dark',.05)
    box('KingLeg',(side*.37,.03,.56),(.48,.49,.36),'Boss_Stone',.035)
    box('KneeMoss',(side*.37,-.26,.69),(.37,.08,.20),'Boss_Moss',.01)
box('KingTorso',(0,0,1.25),(1.36,.82,1.08),'Boss_Stone',.065)
box('BackSlab',(0,.43,1.29),(1.22,.16,.98),'Boss_Slab',.03)
box('KingHead',(0,-.055,2.005),(.90,.68,.46),'Boss_Stone',.035)
box('Neck',(0,0,1.78),(.56,.50,.15),'Boss_Dark',.015)
for side in [-1,1]:
    box('KingEye',(side*.22,-.405,2.035),(.17,.035,.095),'Boss_Core',.006)
    brow=box('KingBrow',(side*.22,-.426,2.15),(.28,.055,.095),'Boss_Dark',.009)
    brow.rotation_euler.y=side*-.13
box('CoreSocket',(0,-.435,1.44),(.43,.075,.40),'Boss_Dark',.025)
box('AmberCore',(0,-.486,1.44),(.25,.035,.25),'Boss_Core',.012)
for name,pos,size in [('SeamA',(.10,-.427,1.20),(.29,.018,.045)),('SeamB',(.245,-.427,1.035),(.045,.018,.36)),
                      ('SeamC',(.34,-.427,.86),(.23,.018,.045))]: box(name,pos,size,'Boss_Seam',.002)
box('CrownBase',(0,0,2.29),(.94,.49,.18),'Boss_Wood',.025)
for x,h in [(-.34,.39),(0,.60),(.34,.32)]:
    box('StumpCrown',(x,0,2.35+h/2),(.25,.40,h),'Boss_Wood',.022)
    box('CrownMoss',(x,-.01,2.35+h+.02),(.28,.42,.055),'Boss_Moss',.009)
for side in [-1,1]:
    arm=pivot('BossLeftArm' if side<0 else 'BossRightArm',(side*.84,0,1.80))
    box('ShoulderSlab',(side*.91,0,1.90),(.65,.94,.61),'Boss_Slab',.085,arm)
    box('ShoulderMoss',(side*.91,-.02,2.225),(.56,.79,.095),'Boss_Moss',.017,arm)
    box('MossLip',(side*.91,-.48,2.11),(.42,.055,.23),'Boss_Moss',.01,arm)
    box('UpperArm',(side*1.0,.01,1.43),(.39,.49,.48),'Boss_Stone',.035,arm)
    box('Forearm',(side*1.08,-.10,1.10),(.58,.64,.52),'Boss_Slab',.055,arm)
    box('KingFist',(side*1.11,-.25,.76),(.64,.71,.40),'Boss_Dark',.045,arm)
    for finger in [-1,1]: box('Knuckle',(side*1.11+finger*.155,-.625,.77),(.27,.13,.33),'Boss_Dark',.024,arm)
    root=box('ShoulderRoot',(side*.72,-.51,1.62),(.105,.10,.42),'Boss_Wood',.018,arm); root.rotation_euler.y=side*.30
    root=box('WaistRoot',(side*.52,-.455,.83),(.34,.12,.10),'Boss_Wood',.012); root.rotation_euler.y=side*.2
    box('BackMoss',(side*.43,.53,1.61 if side<0 else .98),(.25,.08,.29),'Boss_Moss',.013)
pivot('FaceAnchor',(0,-.6,1.9))
bpy.ops.object.select_all(action='DESELECT')
for obj in parts: obj.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/Resources/Characters/ForestStoneKing.fbx'),use_selection=True,
    object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False)

bpy.ops.mesh.primitive_plane_add(size=200)
floor=bpy.context.object; mat=bpy.data.materials.new('Preview Grey'); mat.diffuse_color=(.28,.29,.27,1); floor.data.materials.append(mat)
for loc,power,size in [((3,-5,7),650,5),((-4,-2,4),300,4),((1,4,6),500,4)]:
    bpy.ops.object.light_add(type='AREA',location=loc); lamp=bpy.context.object; lamp.data.energy=power; lamp.data.size=size
    lamp.rotation_euler=(Vector((0,0,1.3))-lamp.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(4,-7,4.8)); camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,1.4))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'; camera.data.ortho_scale=4.35
scene=bpy.context.scene; scene.camera=camera; scene.render.engine='CYCLES'; scene.cycles.samples=32; scene.cycles.use_denoising=True
scene.render.resolution_x=1000; scene.render.resolution_y=1000; scene.render.resolution_percentage=100
scene.world.color=(.18,.18,.18); scene.view_settings.view_transform='AgX'
scene.render.filepath=str(SOURCE/'ForestBoss-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'ForestStoneKing.blend'))
bpy.ops.render.render(write_still=True)
print('FOREST_BOSS_EXPORT_COMPLETE')

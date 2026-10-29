"""Build the approved block-style hooded bomber without changing the existing enemies."""
import bpy, math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
palette = {'Bomber_Plum': (.20,.10,.29), 'Bomber_Edge': (.29,.16,.38), 'Enemy_Dark': (.075,.075,.085), 'Bomber_Belt': (.66,.59,.36), 'Bomber_Pouch': (.20,.13,.08), 'Bomber_Eye': (1,.48,.08), 'Bomber_Spark': (1,.65,.10)}
materials = {}
for name, color in palette.items():
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color,1)
    mat.use_nodes = True
    node = mat.node_tree.nodes['Principled BSDF']
    node.inputs['Base Color'].default_value = (*color,1)
    node.inputs['Roughness'].default_value = .9
    materials[name] = mat
parts = []
def box(name, pos, size, material, bevel=.015, tilt=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    mod = obj.modifiers.new('Flat bevel', 'BEVEL')
    mod.width = bevel
    mod.segments = 1
    bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.rotation_euler.x = tilt
    obj.data.materials.append(materials[material])
    parts.append(obj)
    return obj

for x in [-.17,.17]: box('BomberFoot',(x,0,.08),(.22,.29,.16),'Enemy_Dark')
box('BomberRobe',(0,.01,.33),(.47,.33,.40),'Bomber_Plum',.025)
box('HoodBack',(0,.17,.70),(.64,.14,.48),'Bomber_Plum',.025)
box('HoodCrown',(0,.015,.915),(.67,.49,.16),'Bomber_Plum',.025)
for x in [-.265,.265]: box('HoodSide',(x,-.035,.715),(.14,.49,.46),'Bomber_Edge',.02)
box('Face',(0,-.195,.71),(.40,.035,.30),'Enemy_Dark',.006)
for x in [-.115,.115]: box('BomberEye',(x,-.222,.735),(.105,.024,.066),'Bomber_Eye',.004)
box('Belt',(0,-.01,.32),(.50,.36,.065),'Bomber_Belt',.007)
box('Pouch',(.19,-.225,.32),(.18,.105,.18),'Bomber_Pouch',.01)
box('PouchClasp',(.19,-.284,.33),(.045,.017,.05),'Bomber_Belt',.003)
box('LeftArm',(.32,.0,.40),(.15,.21,.25),'Bomber_Plum',.015,tilt=-.12)
bpy.ops.object.select_all(action='DESELECT')
for obj in parts: obj.select_set(True)
bpy.context.view_layer.objects.active=parts[0]
bpy.ops.object.join()
model=bpy.context.object; model.name='EnemyBomber'
bpy.context.scene.cursor.location=(0,0,0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
parts.clear()
# Articulated throwing arm and held prop use their own named nodes.
bpy.ops.object.empty_add(location=(-.31,0,.49))
pivot=bpy.context.object; pivot.name='ThrowArm'
arm=box('ThrowArmMesh',(-.34,-.055,.38),(.15,.22,.25),'Bomber_Plum',.015,tilt=-.25)
arm.parent=pivot; arm.matrix_parent_inverse=pivot.matrix_world.inverted()
parts.clear()
bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=1,radius=.115,location=(-.35,-.18,.275))
bomb=bpy.context.object; bomb.data.materials.append(materials['Enemy_Dark']); parts.append(bomb)
box('Fuse',(-.35,-.18,.404),(.032,.032,.07),'Bomber_Belt',.003)
box('Spark',(-.35,-.18,.448),(.043,.04,.032),'Bomber_Spark',.003)
bpy.ops.object.select_all(action='DESELECT')
for obj in parts: obj.select_set(True)
bpy.context.view_layer.objects.active=bomb; bpy.ops.object.join()
bomb.name='HeldBomb'; bomb.parent=pivot; bomb.matrix_parent_inverse=pivot.matrix_world.inverted()
bpy.ops.object.empty_add(location=(0,-.3,.7))
anchor=bpy.context.object; anchor.name='FaceAnchor'
bpy.ops.object.select_all(action='DESELECT')
for obj in [model,pivot,arm,bomb,anchor]: obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/Resources/Characters/EnemyBomber.fbx'),
    use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',
    apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
bpy.ops.mesh.primitive_plane_add(size=200)
floor = bpy.context.object
mat = bpy.data.materials.new('Preview Olive')
mat.diffuse_color = (.16,.20,.095,1)
floor.data.materials.append(mat)
for loc, power, size in [((2,-4,6),500,5),((-4,-2,3),200,4),((0,4,5),300,4)]:
    bpy.ops.object.light_add(type='AREA', location=loc)
    lamp = bpy.context.object
    lamp.data.energy = power
    lamp.data.size = size
    lamp.rotation_euler = (Vector((0,0,.5))-lamp.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(1.6,-3.8,2.5))
camera = bpy.context.object
camera.rotation_euler = (Vector((0,0,.52))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 1.7
scene = bpy.context.scene
scene.camera = camera
scene.render.engine = 'CYCLES'
scene.cycles.samples = 32
scene.cycles.use_denoising = True
scene.render.resolution_x = 900
scene.render.resolution_y = 900
scene.render.resolution_percentage = 100
scene.world.color = (.15,.15,.15)
scene.view_settings.view_transform = 'AgX'
scene.render.filepath = str(SOURCE/'Bomber-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'BlockBomber.blend'))
bpy.ops.render.render(write_still=True)
print('BOMBER_EXPORT_COMPLETE triangles=' + str(sum(len(p.vertices)-2 for mesh in [model,arm,bomb] for p in mesh.data.polygons)))

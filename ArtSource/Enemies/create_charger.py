"""Build the approved block-style charging imp without changing the existing enemies."""
import bpy, math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
palette = {'Charger_Rust': (.56,.19,.095), 'Charger_Shadow': (.35,.095,.045),
           'Enemy_Dark': (.075,.10,.13), 'Charger_Horn': (.82,.75,.55),
           'Charger_Eye': (1,.48,.08)}
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

for x in [-.14,.14]:
    box('ChargerFoot',(x,-.03,.08),(.21,.30,.16),'Enemy_Dark')
    box('ChargerLeg',(x,.015,.24),(.12,.15,.22),'Charger_Shadow',tilt=-.15)
box('ChargerBody',(0,-.015,.43),(.35,.29,.33),'Charger_Rust',.025,tilt=-.12)
box('ChargerHead',(0,-.09,.72),(.60,.45,.43),'Charger_Rust',.035,tilt=-.10)
for side in [-1,1]:
    ear = box('ChargerEar',(side*.36,.025,.73),(.20,.17,.16),'Charger_Shadow')
    ear.rotation_euler.z = side*.35
    arm = box('ChargerArm',(side*.235,.025,.44),(.13,.17,.25),'Charger_Shadow',tilt=-.4)
    box('ChargerHorn',(side*.195,-.13,.995),(.105,.14,.19),'Charger_Horn',tilt=.3)
    box('ChargerEye',(side*.14,-.323,.73),(.115,.027,.073),'Charger_Eye',.005)
    brow=box('ChargerBrow',(side*.14,-.332,.792),(.16,.035,.038),'Enemy_Dark',.004)
    brow.rotation_euler.y = -side*.12
box('ChargerMouth',(0,-.344,.58),(.19,.035,.052),'Enemy_Dark',.004)
bpy.ops.object.select_all(action='DESELECT')
for obj in parts: obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'EnemyCharger'
bpy.context.scene.cursor.location = (0,0,0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bpy.ops.object.empty_add(location=(0,-.35,.7))
anchor = bpy.context.object
anchor.name = 'FaceAnchor'
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
anchor.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/Resources/Characters/EnemyCharger.fbx'),
    use_selection=True, object_types={'MESH','EMPTY'}, axis_forward='-Z', axis_up='Y',
    apply_unit_scale=True, bake_space_transform=True, add_leaf_bones=False, bake_anim=False)

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
scene.render.filepath = str(SOURCE/'Charger-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'BlockCharger.blend'))
bpy.ops.render.render(write_still=True)
print('CHARGER_EXPORT_COMPLETE triangles=' + str(sum(len(p.vertices)-2 for p in model.data.polygons)))

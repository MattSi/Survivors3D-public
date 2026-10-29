"""Build the approved block-style stone-armored rockling without changing the existing enemies."""
import bpy, math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
palette = {'StoneArmor_Slab': (.46,.46,.41), 'StoneArmor_Body': (.33,.36,.36), 'Enemy_Dark': (.075,.10,.13), 'StoneArmor_Moss': (.27,.34,.17), 'StoneArmor_Eye': (1,.48,.08)}
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

for side in [-1,1]:
    box('StoneFoot',(side*.235,-.02,.12),(.35,.43,.24),'Enemy_Dark',.025)
    box('StoneLeg',(side*.235,.03,.30),(.24,.28,.20),'StoneArmor_Body')
box('StoneChest',(0,-.025,.59),(.78,.49,.52),'StoneArmor_Body',.055)
box('StoneHead',(0,-.04,.97),(.58,.45,.30),'StoneArmor_Body',.025)
box('MossCrown',(-.10,.025,1.15),(.25,.34,.07),'StoneArmor_Moss')
for side in [-1,1]:
    box('ShoulderJoint',(side*.49,.01,.83),(.22,.28,.20),'Enemy_Dark')
    shoulder=box('ShoulderSlab',(side*.51,.015,1.005),(.35,.60,.42),'StoneArmor_Slab',.065)
    shoulder.rotation_euler.y=side*.10
    box('Forearm',(side*.56,-.015,.59),(.29,.37,.38),'StoneArmor_Slab',.04)
    box('Fist',(side*.56,-.065,.405),(.27,.32,.18),'Enemy_Dark',.02)
    box('ShoulderMoss',(side*.52,-.02,1.22),(.19,.26,.018),'StoneArmor_Moss',.004)
    box('StoneEye',(side*.14,-.276,.995),(.105,.025,.065),'StoneArmor_Eye',.004)
box('StoneMouth',(0,-.276,.883),(.22,.026,.045),'Enemy_Dark',.003)
# A small stepped seam, rather than a detailed fracture texture.
box('SeamTop',(.07,-.277,.775),(.027,.016,.13),'Enemy_Dark',.002)
box('SeamStep',(.097,-.277,.71),(.08,.016,.027),'Enemy_Dark',.002)
box('SeamBottom',(.125,-.277,.555),(.027,.016,.29),'Enemy_Dark',.002)
box('BackSlab',(0,.27,.65),(.69,.10,.54),'StoneArmor_Slab',.025)
bpy.ops.object.select_all(action='DESELECT')
for obj in parts: obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
model = bpy.context.object
model.name = 'EnemyStoneArmor'
bpy.context.scene.cursor.location = (0,0,0)
bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bpy.ops.object.empty_add(location=(0,-.4,.8))
anchor = bpy.context.object
anchor.name = 'FaceAnchor'
bpy.ops.object.select_all(action='DESELECT')
model.select_set(True)
anchor.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/Resources/Characters/EnemyStoneArmor.fbx'),
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
camera.data.ortho_scale = 2.15
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
scene.render.filepath = str(SOURCE/'StoneArmor-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'BlockStoneArmor.blend'))
bpy.ops.render.render(write_still=True)
print('STONE_ARMOR_EXPORT_COMPLETE triangles=' + str(sum(len(p.vertices)-2 for p in model.data.polygons)))

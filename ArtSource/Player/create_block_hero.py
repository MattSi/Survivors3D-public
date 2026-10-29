"""Blender source generator for the playable low-poly block hero."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/Resources/Characters'
OUT.mkdir(parents=True, exist_ok=True)
SOURCE = Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
palette = {
    'Hero_Skin': (0.92, 0.63, 0.36), 'Hero_Hair': (0.095, 0.043, 0.025),
    'Hero_Tunic': (0.64, 0.025, 0.045), 'Hero_Scarf': (0.98, 0.12, 0.065),
    'Hero_Gold': (0.95, 0.55, 0.12), 'Hero_Leather': (0.19, 0.075, 0.035),
    'Hero_Boots': (0.07, 0.075, 0.10), 'Hero_Eyes': (0.018, 0.016, 0.02),
    'Hero_Shirt': (0.92, 0.80, 0.57)
}
materials = {}
for name, color in palette.items():
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = (*color, 1)
    bsdf.inputs['Roughness'].default_value = 0.78
    materials[name] = m

parts = []
def box(name, pos, size, material, bevel=0.015, rotation=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Small flat bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.rotation_euler.y = math.radians(rotation)
    obj.data.materials.append(materials[material])
    parts.append(obj)
    return obj

for sign, label in [(-1, 'Left'), (1, 'Right')]:
    box(label+'Boot', (sign*.18, -.015, .12), (.27, .38, .24), 'Hero_Boots', .025)
    box(label+'BootCuff', (sign*.18, 0, .255), (.255, .30, .09), 'Hero_Leather')
box('Tunic', (0, 0, .51), (.64, .41, .52), 'Hero_Tunic', .035)
box('TunicHem', (0, -.005, .285), (.66, .43, .055), 'Hero_Gold', .008)
box('Belt', (0, -.008, .40), (.675, .435, .105), 'Hero_Leather', .012)
box('Buckle', (0, -.236, .40), (.16, .036, .135), 'Hero_Gold', .008)
box('BuckleInset', (0, -.258, .40), (.083, .018, .068), 'Hero_Leather', .003)
box('Collar', (0, -.008, .75), (.58, .43, .09), 'Hero_Scarf')
box('ScarfTail', (.245, -.249, .635), (.115, .045, .20), 'Hero_Scarf', .009, -10)
box('ChestPanel', (-.045, -.214, .60), (.26, .018, .16), 'Hero_Tunic', .006)
for z in [.58, .66]: box('CoatStud', (-.1, -.23, z), (.035, .022, .035), 'Hero_Gold', .005)
for sign, label in [(-1, 'Left'), (1, 'Right')]:
    box(label+'Sleeve', (sign*.413, 0, .655), (.205, .355, .245), 'Hero_Tunic', .025)
    box(label+'ShoulderTrim', (sign*.413, -.005, .745), (.225, .37, .047), 'Hero_Gold', .008)
    box(label+'Arm', (sign*.418, -.015, .435), (.17, .27, .23), 'Hero_Skin', .02)
    box(label+'WristWrap', (sign*.418, -.019, .355), (.186, .287, .075), 'Hero_Leather', .012)
head_start = len(parts)
box('Head', (0, 0, 1.055), (.81, .585, .595), 'Hero_Skin', .032)
box('HairCap', (0, .005, 1.354), (.855, .625, .17), 'Hero_Hair', .025)
box('HairBack', (0, .281, 1.135), (.815, .075, .34), 'Hero_Hair', .013)
box('FringeLeft', (-.22, -.302, 1.29), (.375, .052, .07), 'Hero_Hair', .008)
box('FringeRight', (.19, -.302, 1.294), (.45, .052, .064), 'Hero_Hair', .008)
for sign in [-1, 1]:
    box('Sideburn', (sign*.382, -.217, 1.16), (.065, .13, .23), 'Hero_Hair', .007)
    box('Eyebrow', (sign*.16, -.303, 1.185), (.181, .032, .046), 'Hero_Hair', .004, sign*-5)
    box('Eye', (sign*.16, -.307, 1.105), (.08, .031, .099), 'Hero_Eyes', .012)
box('Nose', (0, -.312, 1.035), (.072, .061, .072), 'Hero_Skin', .012)
box('Mouth', (0, -.305, .952), (.133, .024, .032), 'Hero_Eyes', .004)

# An explicit gameplay anchor is exported with the model.
bpy.ops.object.empty_add(type='PLAIN_AXES', location=(0, -.33, .965))
mouth = bpy.context.object
mouth.name = 'MouthAnchor'
parts.append(mouth)
# Raise the face toward a top-down camera without leaning the body.
pivot = Vector((0, 0, .79))
head_tilt = Matrix.Translation(pivot) @ Matrix.Rotation(math.radians(-15), 4, 'X') @ Matrix.Translation(-pivot)
for part in parts[head_start:]:
    part.matrix_world = head_tilt @ part.matrix_world
bpy.ops.object.select_all(action='DESELECT')
for obj in parts: obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.export_scene.fbx(filepath=str(OUT/'BlockHero.fbx'), use_selection=True,
    object_types={'MESH', 'EMPTY'}, axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
    bake_space_transform=True, add_leaf_bones=False, bake_anim=False, path_mode='AUTO')

# Presentation set lives only in the .blend, never in the exported game asset.
ground_mat = bpy.data.materials.new('Preview Ground')
ground_mat.diffuse_color = (.055, .085, .105, 1)
bpy.ops.mesh.primitive_plane_add(size=200)
ground = bpy.context.object
ground.name = 'Preview Ground'
ground.data.materials.append(ground_mat)
def light(name, pos, energy, size):
    bpy.ops.object.light_add(type='AREA', location=pos)
    obj=bpy.context.object
    obj.name=name
    obj.data.energy=energy
    obj.data.shape='DISK'
    obj.data.size=size
    obj.rotation_euler=(Vector((0,0,.8))-obj.location).to_track_quat('-Z','Y').to_euler()
light('Key', (3,-4,6), 450, 4)
light('Fill', (-3,-2,3), 220, 3)
light('Rim', (1,3,4), 350, 3)
bpy.ops.object.camera_add(location=(1.2,-4,6.7))
camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,.73))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'
camera.data.ortho_scale=2.2
scene=bpy.context.scene
scene.camera=camera
scene.render.engine='CYCLES'
scene.cycles.samples=32
scene.cycles.use_denoising=True
scene.render.resolution_x=1000
scene.render.resolution_y=1000
scene.render.resolution_percentage=100
scene.world.color=(.16,.16,.16)
scene.view_settings.view_transform='AgX'
scene.render.image_settings.file_format='PNG'
scene.render.filepath=str(SOURCE/'BlockHero-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'BlockHero.blend'))
mesh_parts=[p for p in parts if p.type=='MESH']
(SOURCE/'model-info.json').write_text(json.dumps({'mesh_parts':len(mesh_parts),
    'triangles':sum(sum(len(poly.vertices)-2 for poly in p.data.polygons) for p in mesh_parts),
    'height':1.439,'palette':palette},indent=2))
bpy.ops.render.render(write_still=True)
print('BLOCK_HERO_EXPORT_COMPLETE')

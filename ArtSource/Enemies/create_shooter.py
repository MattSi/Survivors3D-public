"""Build the approved block-style bone needle shooter and its articulated firing arm."""
import bpy
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
palette = {'Shooter_Sage': (.32, .40, .34), 'Shooter_Bone': (.78, .75, .61),
           'Enemy_Dark': (.075, .10, .13), 'Shooter_Eye': (1, .48, .08), 'Shooter_Core': (1, .48, .08)}
materials = {}
for name, color in palette.items():
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*color, 1)
    mat.use_nodes = True
    node = mat.node_tree.nodes['Principled BSDF']
    node.inputs['Base Color'].default_value = (*color, 1)
    node.inputs['Roughness'].default_value = .9
    materials[name] = mat
parts = []

def box(name, pos, size, material, bevel=.012):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = obj.modifiers.new('Flat bevel', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.materials.append(materials[material])
    parts.append(obj)
    return obj

def join(name):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    parts.clear()
    return obj

for x in [-.14, .14]:
    box('Foot', (x, -.035, .08), (.22, .29, .16), 'Enemy_Dark')
box('Body', (0, .01, .335), (.43, .32, .35), 'Shooter_Sage', .018)
box('SquareHead', (0, .015, .735), (.65, .48, .47), 'Shooter_Sage', .025)
box('Face', (0, -.233, .705), (.43, .03, .275), 'Enemy_Dark', .005)
box('BoneBrow', (0, -.263, .855), (.47, .045, .075), 'Shooter_Bone', .008)
for x in [-.115, .115]:
    box('AmberEye', (x, -.258, .737), (.095, .025, .065), 'Shooter_Eye', .004)
for x in [-.335, .335]:
    box('HeadSide', (x, .01, .70), (.10, .30, .30), 'Shooter_Sage')
box('LeftArm', (.295, .01, .37), (.15, .21, .25), 'Shooter_Sage')
box('DartRack', (0, .30, .84), (.49, .14, .22), 'Shooter_Sage')
for x in [-.16, 0, .16]:
    box('DartShaft', (x, .30, .965), (.055, .055, .20), 'Shooter_Bone', 0)
    # A five-sided pencil head: broad flat faces keep the small silhouette legible.
    vertices = [(x+dx, .30+dy, z) for z in [1.00, 1.105] for dx, dy in [(-.045,-.045),(.045,-.045),(.045,.045),(-.045,.045)]]
    vertices.append((x, .30, 1.16))
    faces = [(3,2,1,0)] + [(i,(i+1)%4,(i+1)%4+4,i+4) for i in range(4)] + [(i+4,(i+1)%4+4,8) for i in range(4)]
    mesh = bpy.data.meshes.new('BoneDart')
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(materials['Shooter_Bone'])
    obj = bpy.data.objects.new('RackDart', mesh)
    bpy.context.collection.objects.link(obj)
    parts.append(obj)
body = join('EnemyShooter')

bpy.ops.object.empty_add(location=(-.295, .015, .46))
pivot = bpy.context.object
pivot.name = 'ShootArm'
box('Arm', (-.325, -.055, .46), (.14, .23, .14), 'Shooter_Sage')
box('BoneLauncher', (-.325, -.275, .46), (.235, .32, .23), 'Shooter_Bone', .023)
box('SquareMuzzle', (-.325, -.44, .46), (.155, .018, .15), 'Enemy_Dark', .008)
box('MuzzleCore', (-.325, -.452, .46), (.069, .012, .066), 'Shooter_Core', .005)
arm = join('ShootArmMesh')
arm.parent = pivot
arm.matrix_parent_inverse = pivot.matrix_world.inverted()
anchors = []
for name, location in [('Muzzle', (-.325, -.47, .46)), ('Breech', (-.325, -.16, .46))]:
    bpy.ops.object.empty_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.parent = pivot
    obj.matrix_parent_inverse = pivot.matrix_world.inverted()
    anchors.append(obj)
bpy.ops.object.empty_add(location=(0, -.3, .7))
face = bpy.context.object
face.name = 'FaceAnchor'
bpy.ops.object.select_all(action='DESELECT')
for obj in [body, pivot, arm, face, *anchors]:
    obj.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(ROOT/'Assets/Resources/Characters/EnemyShooter.fbx'),
    use_selection=True, object_types={'MESH','EMPTY'}, axis_forward='-Z', axis_up='Y',
    apply_unit_scale=True, bake_space_transform=True, add_leaf_bones=False, bake_anim=False)

# Save a useful idle pose in the editable studio scene; the runtime poses the exported neutral arm.
pivot.rotation_euler.x = .5
bpy.ops.mesh.primitive_plane_add(size=200)
floor = bpy.context.object
mat = bpy.data.materials.new('Preview Gray')
mat.diffuse_color = (.35, .35, .35, 1)
floor.data.materials.append(mat)
for loc, power, size in [((2,-4,6),500,5),((-4,-2,3),200,4),((0,4,5),300,4)]:
    bpy.ops.object.light_add(type='AREA', location=loc)
    lamp = bpy.context.object
    lamp.data.energy = power
    lamp.data.size = size
    lamp.rotation_euler = (Vector((0,0,.5))-lamp.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(1.6,-3.8,2.5))
camera = bpy.context.object
camera.rotation_euler = (Vector((0,0,.56))-camera.location).to_track_quat('-Z','Y').to_euler()
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
scene.render.filepath = str(SOURCE/'Shooter-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'BlockShooter.blend'))
bpy.ops.render.render(write_still=True)
print('SHOOTER_EXPORT_COMPLETE triangles=' + str(sum(len(p.vertices)-2 for obj in [body,arm] for p in obj.data.polygons)))

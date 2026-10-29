"""Build the approved teal, cracked splitter and its smaller non-splitting offspring."""
import bpy
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).resolve().parent
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
palette = {'Splitter_Teal': (.24,.43,.45), 'Splitter_Shadow': (.11,.24,.27),
           'Splitter_Crack': (.78,.91,.26), 'Splitter_Eye': (.96,.98,.48)}
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
def box(name, pos, size, material, bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=pos)
    obj=bpy.context.object
    obj.name=name
    obj.dimensions=size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    mod=obj.modifiers.new('Flat bevel','BEVEL'); mod.width=bevel; mod.segments=1
    bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.materials.append(materials[material]); parts.append(obj)

def crack(points, width, side=False):
    # Broad flat ribbons on the bevelled surfaces, without texture dependencies.
    verts=[]
    for p in points:
        offset=Vector((0,width/2,0) if side else (width/2,0,0))
        verts.extend([Vector(p)-offset,Vector(p)+offset])
    faces=[(i*2,i*2+1,i*2+3,i*2+2) for i in range(len(points)-1)]
    for i, face in enumerate(faces):
        a,b,c=[verts[j] for j in face[:3]]
        outward=Vector((1,0,0)) if side else Vector((0,-1,1))
        if (b-a).cross(c-a).dot(outward)<0: faces[i]=tuple(reversed(face))
    mesh=bpy.data.meshes.new('Crack ribbon'); mesh.from_pydata(verts,[],faces); mesh.update()
    obj=bpy.data.objects.new('Luminous fissure',mesh); bpy.context.collection.objects.link(obj)
    obj.data.materials.append(materials['Splitter_Crack']); parts.append(obj)

def build(name, child=False):
    parts.clear()
    box('Swollen body',(0,0,.72),(1,.78,1),'Splitter_Teal',.14)
    box('Dark face',(0,-.398,.53),(.65,.07,.36),'Splitter_Shadow',.025)
    for sign in [-1,1]:
        box('Foot',(sign*.23,-.035,.12),(.30,.39,.24),'Splitter_Shadow')
        box('Stub arm',(sign*.53,.0,.51),(.23,.34,.32),'Splitter_Teal',.065)
        box('Rectangular eye',(sign*.18,-.441,.59),(.15,.025,.085),'Splitter_Eye',.008)
    # Continuous top-to-face seam follows the crown's front bevel.
    crack([(-.12,.22,1.225),(-.02,.02,1.225),(-.11,-.25,1.225),
           (.04,-.395,1.08),(-.05,-.395,.89),(.015,-.395,.73)],.055 if child else .075)
    if not child:
        crack([(.505,.14,.95),(.505,-.05,.79),(.505,.025,.58),(.505,-.10,.42)],.05,True)
        crack([(-.31,-.395,1.02),(-.39,-.395,.86),(-.32,-.395,.70),(-.40,-.395,.45)],.04)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts: obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]; bpy.ops.object.join()
    model=bpy.context.object; model.name=name
    bpy.context.scene.cursor.location=(0,0,0); bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    # Child source geometry is smaller too; runtime normalizes to its own gameplay height.
    if child:
        model.scale=(.5,.5,.5)
        bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bpy.ops.object.empty_add(location=(0,-.3,.4))
    anchor=bpy.context.object; anchor.name='FaceAnchor'
    bpy.ops.object.select_all(action='DESELECT'); model.select_set(True); anchor.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(ROOT/('Assets/Resources/Characters/'+name+'.fbx')),
        use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',
        apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
    bpy.data.objects.remove(anchor,do_unlink=True)
    print(name+' triangles='+str(sum(len(p.vertices)-2 for p in model.data.polygons)))
    return model

parent=build('EnemySplitter'); child=build('EnemySplitling',True)
parent.location.x=-.45; child.location=(.65,-.1,0)
other=child.copy(); other.data=child.data; bpy.context.collection.objects.link(other); other.location=(.95,.65,0)
bpy.ops.mesh.primitive_plane_add(size=200)
floor=bpy.context.object; mat=bpy.data.materials.new('Preview Olive'); mat.diffuse_color=(.16,.20,.095,1); floor.data.materials.append(mat)
for loc,power,size in [((2,-4,6),500,5),((-4,-2,3),200,4),((0,4,5),300,4)]:
    bpy.ops.object.light_add(type='AREA',location=loc)
    lamp=bpy.context.object; lamp.data.energy=power; lamp.data.size=size
    lamp.rotation_euler=(Vector((0,0,.5))-lamp.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(2.6,-4.6,3))
camera=bpy.context.object; camera.rotation_euler=(Vector((.05,.15,.55))-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.type='ORTHO'; camera.data.ortho_scale=2.9
scene=bpy.context.scene; scene.camera=camera; scene.render.engine='CYCLES'; scene.cycles.samples=32; scene.cycles.use_denoising=True
scene.render.resolution_x=1000; scene.render.resolution_y=850; scene.render.resolution_percentage=100
scene.world.color=(.15,.15,.15); scene.view_settings.view_transform='AgX'
scene.render.filepath=str(SOURCE/'Splitter-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'BlockSplitter.blend'))
bpy.ops.render.render(write_still=True)
print('SPLITTER_EXPORT_COMPLETE')

import bpy,json,math
from pathlib import Path
from mathutils import Vector,Matrix
ROOT=Path(__file__).resolve().parents[2]
layout=json.loads((ROOT/'ArtSource/World/layout.json').read_text())
bounds=json.loads((ROOT/'ArtSource/World/model-bounds.json').read_text())
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
mats={}
for guid,info in layout['materials'].items():
 mat=bpy.data.materials.new(guid);mat.use_nodes=True
 shader=mat.node_tree.nodes['Principled BSDF'];shader.inputs['Roughness'].default_value=.9
 if 'texture' in info:
  tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(ROOT/info['texture']),check_existing=True)
  mat.node_tree.links.new(tex.outputs['Color'],shader.inputs['Base Color'])
 else:shader.inputs['Base Color'].default_value=(*info['color'],1)
 mats[guid]=mat
templates={}
for short in {r['prefab'] for r in layout['objects'] if 'prefab' in r}:
 before=set(bpy.context.scene.objects)
 bpy.ops.import_scene.fbx(filepath=str(ROOT/bounds[short]['fbx']))
 imported=list(set(bpy.context.scene.objects)-before)
 data=[]
 for obj in imported:
  if obj.type=='MESH':
   mesh=obj.data.copy();mesh.transform(obj.matrix_world);data.append(mesh)
 for obj in imported:bpy.data.objects.remove(obj,do_unlink=True)
 templates[short]=data
for record in layout['objects']:
 x,y,z=record['pos'];sx,sy,sz=record['scale']
 if record.get('cube'):
  bpy.ops.mesh.primitive_cube_add(size=1,location=(x,-z,y));obj=bpy.context.object
  obj.dimensions=(sx,sz,sy);obj.name=record['name'];obj.data.materials.append(mats[record['material']])
 else:
  for mesh in templates[record['prefab']]:
   obj=bpy.data.objects.new(record['prefab'],mesh.copy());bpy.context.collection.objects.link(obj)
   obj.location=(x,-z,y);obj.scale=(sx,sz,sy);obj.rotation_euler.z=math.radians(record['yaw'])
   obj.data.materials.clear()
   for guid in record['materials']:obj.data.materials.append(mats[guid])
   for poly in obj.data.polygons:poly.material_index=min(poly.material_index,len(record['materials'])-1)
world=bpy.context.scene.world;world.color=(.2,.2,.2)
bpy.ops.object.light_add(type='SUN',location=(0,0,30));sun=bpy.context.object;sun.rotation_euler=(.45,-.35,-.4);sun.data.energy=2;sun.data.angle=.15
bpy.ops.object.light_add(type='AREA',location=(0,-15,40));bpy.context.object.data.energy=1700;bpy.context.object.data.size=45
bpy.ops.object.camera_add(location=(42,-58,62));camera=bpy.context.object
camera.rotation_euler=(Vector((0,0,0))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=82
scene=bpy.context.scene;scene.camera=camera;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1400;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX';scene.render.film_transparent=True
scene.render.filepath=str(ROOT/'ArtSource/World/ForestCamp-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ArtSource/World/ForestCamp-preview.blend'))
bpy.ops.render.render(write_still=True)
print('FOREST_PREVIEW_COMPLETE')

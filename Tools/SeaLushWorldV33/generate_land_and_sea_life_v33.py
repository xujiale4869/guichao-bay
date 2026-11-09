"""Generate ten coastal ecology and village models in metre-scale Y-up coordinates."""
import argparse
import json
import math
import os
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Euler, Matrix, Vector

ROLES = ("Foliage", "Timber", "Stone", "Rope", "Iron", "Ember")
SLOT_COLORS = (
    (.30,.49,.28,1), (.51,.34,.22,1), (.46,.47,.40,1),
    (.65,.52,.36,1), (.22,.23,.21,1), (1,.36,.05,1),
)
MANIFEST = []


def clear_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def bake_vertex_ao_and_export(bm, name, filepath):
    bm.normal_update()
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    mesh.calc_loop_triangles()
    uv_data = mesh.uv_layers["UVMap"].data
    colors = mesh.color_attributes["Col"].data
    xyz, normals, uv, rgba = [], [], [], []
    triangles = [[] for _ in ROLES]
    vertices = {}
    for face in mesh.loop_triangles:
        normal = mesh.polygons[face.polygon_index].normal
        for loop in face.loops:
            if loop not in vertices:
                vertices[loop] = len(xyz)//3
                xyz.extend(round(float(v),5) for v in mesh.vertices[mesh.loops[loop].vertex_index].co)
                n = mesh.corner_normals[loop].vector if mesh.polygons[face.polygon_index].use_smooth else normal
                normals.extend(round(float(v),5) for v in n)
                uv.extend(round(float(v),5) for v in uv_data[loop].uv)
                rgba.extend(round(float(v),4) for v in colors[loop].color_srgb)
            triangles[face.material_index].append(vertices[loop])
    parts = [{"role":ROLES[i],"triangles":t} for i,t in enumerate(triangles) if t]
    data = {"name":name,"vertices":xyz,"normals":normals,"uv":uv,"rgba":rgba,"parts":parts}
    source_dir.mkdir(parents=True,exist_ok=True)
    (source_dir/(name+".json")).write_text(json.dumps(data,separators=(",",":")),encoding="utf-8")
    role_dir.mkdir(parents=True,exist_ok=True)
    (role_dir/(name+".json")).write_text(
        json.dumps({"parts":[{"role":p["role"]} for p in parts]}),encoding="utf-8")

    for v in mesh.vertices:
        x,y,z = v.co
        v.co = (x,-z,y)
    mesh.update()
    obj = bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    for role,tint in zip(ROLES,SLOT_COLORS):
        material = bpy.data.materials.new(role)
        material.diffuse_color = tint
        obj.data.materials.append(material)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=filepath,use_selection=True,object_types={'MESH'},
        apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',
        add_leaf_bones=False,bake_anim=False)
    MANIFEST.append({"name":name,"triangles":len(mesh.loop_triangles),
                     "vertices":len(xyz)//3,"roles":[p["role"] for p in parts],
                     "boundsMetres":[[min(xyz[a::3]) for a in range(3)],[max(xyz[a::3]) for a in range(3)]]})


def add_curved_tube(bm, uv_layer, col_layer, pts, rads, segs=8, rgb=(0.8, 0.7, 0.6), role=1, closed=False):
    rings = []
    n = len(pts)
    for i, (pt, rad) in enumerate(zip(pts, rads)):
        t = i / float(max(1, n - 1))
        p = Vector(pt)
        if closed:
            tang = (Vector(pts[(i+1)%n])-Vector(pts[(i-1)%n])).normalized()
        else:
            tang = (Vector(pts[min(i+1,n-1)])-Vector(pts[max(i-1,0)])).normalized()
        up_ref = Vector((1, 0, 0)) if abs(tang.y) > 0.9 else Vector((0, 1, 0))
        s1 = tang.cross(up_ref).normalized()
        s2 = tang.cross(s1).normalized()
        ring = []
        for s in range(segs):
            ang = (s / float(segs)) * math.pi * 2.0
            v = bm.verts.new(p + (s1 * math.cos(ang) + s2 * math.sin(ang)) * rad)
            ring.append(v)
        rings.append((ring, t))

    for i in range(n if closed else n-1):
        r0, t0 = rings[i]
        r1, t1 = rings[(i+1)%n]
        for s in range(segs):
            sn = (s + 1) % segs
            f = bm.faces.new((r0[s], r0[sn], r1[sn], r1[s]))
            f.material_index = role
            f.smooth = True
            for loop in f.loops:
                th = t0 if loop.vert in r0 else t1
                loop[uv_layer].uv = (th, (s + (loop.vert in (r0[sn],r1[sn])))/segs)
                loop[col_layer] = (rgb[0], rgb[1], rgb[2], 0.38 + 0.62 * th)

    if not closed:
        for ring,t in ((list(reversed(rings[0][0])),0.0),(rings[-1][0],1.0)):
            face = bm.faces.new(ring)
            face.material_index = role
            for loop in face.loops:
                loop[uv_layer].uv = (loop.vert.co.x,loop.vert.co.z)
                loop[col_layer] = (*rgb,.55+.40*t)


def add_organic_mound(bm, uv_layer, col_layer, center, radii, subdiv=2, seed=1.0, rgb=(0.4, 0.7, 0.35), role=0):
    ret = bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=1.0)
    cx, cy, cz = center
    rx, ry, rz = radii
    for v in ret["verts"]:
        p = v.co.copy()
        n = 1.0 + 0.16 * math.sin(p.x * 5.0 + seed) * math.cos(p.z * 5.0 - seed) + 0.10 * math.sin(p.y * 7.0 + seed)
        v.co = Vector((cx + p.x * rx * n, cy + p.y * ry * n, cz + p.z * rz * n))
        for loop in v.link_loops:
            loop.face.material_index = role
            loop.face.smooth = True
            h01 = max(0.0, min(1.0, (p.y + 1.0) * 0.5))
            loop[uv_layer].uv = ((p.x + 1.0) * 0.5, h01)
            tint = Vector(rgb) * (.82+.18*h01)
            if role == 2:
                moss = max(0, min(1, (p.y-.08)*1.25 + .20*math.sin(p.x*8+seed)*math.cos(p.z*9)))
                tint = tint.lerp(Vector((.49,.60,.40)),moss*.68)
            loop[col_layer] = (*tint,.45+.55*h01)


def add_leaf(bm, uv, colors, start, end, width, rgb, role=0):
    # The raised centre rib keeps a visible profile across several camera angles.
    start,end = Vector(start),Vector(end)
    along = end-start
    side = Vector((-along.z,0,along.x)).normalized()*width
    root = bm.verts.new(start)
    left = bm.verts.new(start+along*.46-side)
    ridge = bm.verts.new(start+along*.47+Vector((0,width*.28,0)))
    right = bm.verts.new(start+along*.46+side)
    tip = bm.verts.new(end)
    for verts in ((root,left,ridge),(left,tip,ridge),(root,ridge,right),(ridge,tip,right)):
        face = bm.faces.new(verts)
        face.material_index = role
        for loop in face.loops:
            t = max(0,min(1,(loop.vert.co-start).dot(along)/along.length_squared))
            loop[uv].uv = (0,t)
            loop[colors] = (*(c*(.87+.13*t) for c in rgb),.65+.33*t)


def add_fern_frond(bm, uv, colors, origin, yaw, pitch, length, width, rgb, fern=True):
    axis = Vector((math.cos(yaw),0,math.sin(yaw)))
    side = Vector((-axis.z,0,axis.x))
    origin = Vector(origin)
    def center(t):
        return origin+axis*t*length+Vector((0,math.sin(t*math.pi*.76)*length*math.sin(pitch)-t*t*length*.14,0))
    if not fern:
        samples = [center(i/4) for i in range(5)]
        prev = None
        for i,p in enumerate(samples):
            t = i/4
            w = width*(.26+.82*math.sin(math.pi*t)) if i != 4 else width*.03
            ring = (bm.verts.new(p-side*w),bm.verts.new(p+Vector((0,w*.28,0))),bm.verts.new(p+side*w))
            if prev:
                for k in range(2):
                    face = bm.faces.new((prev[k],prev[k+1],ring[k+1],ring[k]))
                    for loop in face.loops:
                        u = t if loop.vert in ring else t-.25
                        loop[uv].uv = (0,u)
                        loop[colors] = (*(c*(.78+.22*u) for c in rgb),.42+.56*u)
            prev = ring
        return
    add_curved_tube(bm,uv,colors,[center(i/6) for i in range(7)],[.011,.010,.009,.008,.006,.004,.0015],5,rgb,0)
    for j in range(2,10):
        t = j/10
        spread = width*(.80+.22*math.sin(t*math.pi))*(1.28-t)
        for sign in (-1,1):
            base = center(t)
            tip = center(min(.99,t+.14))+side*(sign*spread)
            add_leaf(bm,uv,colors,base,tip,spread*.24,rgb)
    add_leaf(bm,uv,colors,center(.84),center(1.04),width*.18,rgb)


def add_daisy(bm, uv, colors, x, z, height, index):
    top = Vector((x+.065,height,z+.045))
    add_curved_tube(bm,uv,colors,[(x,-.025,z),(x+.025,height*.56,z),top],
                    [.012,.009,.007],5,(.40,.52,.23),0)
    add_organic_mound(bm,uv,colors,top,(.038,.021,.038),1,index,(.96,.69,.24))
    petals = (.94,.87,.65) if index%3 else (.82,.61,.35)
    for i in range(6):
        a = i*math.tau/6
        d = Vector((math.cos(a),0,math.sin(a)))
        add_leaf(bm,uv,colors,top+d*.026,top+d*.100+Vector((0,.014,0)),.024,petals)


def add_beveled_box(bm, uv_layer, col_layer, center, size, rot_euler=(0, 0, 0), bevel=0.02, rgb=(0.75, 0.62, 0.48), role=1):
    rot = Euler(rot_euler, 'XYZ').to_matrix().to_4x4()
    mat = Matrix.Translation(Vector(center)) @ rot @ Matrix.Diagonal((size[0], size[1], size[2], 1.0))
    prior_faces = set(bm.faces)
    ret = bmesh.ops.create_cube(bm,size=1.0,matrix=mat)
    if bevel > .001:
        edges = {edge for vertex in ret["verts"] for edge in vertex.link_edges}
        bmesh.ops.bevel(bm,geom=list(edges),offset=min(bevel,min(size)*.28),
                       segments=1,affect='EDGES',clamp_overlap=True)
    faces = [face for face in bm.faces if face not in prior_faces]
    axis = max(range(3),key=lambda a:size[a])
    for face in faces:
        if not face.is_valid:
            continue
        face.material_index = role
        face.normal_update()
        across = (axis+1)%3 if abs(face.normal[(axis+1)%3]) < .85 else (axis+2)%3
        for loop in face.loops:
            v = loop.vert.co
            loop[uv_layer].uv = (v[axis],v[across])
            loop[col_layer] = (*rgb, .63 if face.normal.y < -.1 else .90)


def generate_all_v33_assets(models_dir):
    # =================================================================
    # 1. Land_0_CoastalPineTree_Trunk.fbx & Canopy.fbx (海风苍劲黑松树 - 双网格分离方便独立赋材质)
    # =================================================================
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    col = bm.loops.layers.color.new("Col")
    # 苍劲曲折的主树干 + 3 根伸展侧枝
    trunk_pts = [(0.0, -0.2, 0.0), (0.18, 0.9, 0.08), (-0.12, 2.1, -0.06), (0.22, 3.3, 0.12), (0.05, 4.3, 0.04)]
    add_curved_tube(bm, uv, col, trunk_pts, [0.36, 0.28, 0.22, 0.15, 0.08], 9, (0.45, 0.34, 0.25))
    add_curved_tube(bm, uv, col, [(-0.08, 1.8, -0.04), (-0.85, 2.3, 0.35), (-1.45, 2.5, 0.55)], [0.14, 0.09, 0.04], 7, (0.45, 0.34, 0.25))
    add_curved_tube(bm, uv, col, [(0.15, 2.6, 0.08), (0.95, 3.1, -0.28), (1.55, 3.3, -0.45)], [0.13, 0.08, 0.04], 7, (0.45, 0.34, 0.25))
    bake_vertex_ao_and_export(bm, "Land_0_CoastalPineTrunk", os.path.join(models_dir, "Land_0_CoastalPineTrunk.fbx"))

    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    col = bm.loops.layers.color.new("Col")
    # 5 团高低错落、迎海风层叠展开的蓬松松针云团
    pads = [
        ((-1.35, 2.65, 0.50), (1.35, 0.52, 1.25), 1.1),
        ((1.45, 3.40, -0.40), (1.40, 0.55, 1.30), 2.3),
        ((0.05, 4.45, 0.05), (1.75, 0.68, 1.65), 3.7),
        ((-0.55, 3.85, -0.65), (1.25, 0.48, 1.18), 4.9),
        ((0.65, 4.10, 0.72), (1.20, 0.46, 1.15), 6.1),
    ]
    for center,rads,seed in pads:
        center = Vector(center)
        add_organic_mound(bm,uv,col,center,(rads[0]*.57,rads[1]*.51,rads[2]*.57),
                          2,seed,(.18,.31,.20))
        for k in range(25):
            angle = k*2.39996+seed
            direction = Vector((math.cos(angle),0,math.sin(angle)))
            across = Vector((-direction.z,0,direction.x))
            reach = .43+.54*((k*7)%17)/17
            radius = Vector((direction.x*rads[0],0,direction.z*rads[2]))
            y = rads[1]*(.16+.47*math.sin(k*.81+seed))
            start = center+radius*(reach*.37)+Vector((0,y-.12,0))
            end = center+radius*reach+Vector((0,y,0))
            tint = (.19+.014*(k%4),.34+.015*(k%5),.21+.009*(k%3))
            for j in range(5):
                t = .24+j*.17
                base = start.lerp(end,t)
                length = .29-.14*t
                for sign in (-1,1):
                    tip = base+direction*length+across*(sign*length*.66)+Vector((0,.09+.04*(j%2),0))
                    add_leaf(bm,uv,col,base,tip,.041+(k%3)*.006,tint)
            add_leaf(bm,uv,col,end-direction*.23,end+direction*.18+Vector((0,.12,0)),.061,tint)
    bake_vertex_ao_and_export(bm, "Land_0_CoastalPineCanopy", os.path.join(models_dir, "Land_0_CoastalPineCanopy.fbx"))

    # =================================================================
    # 2. Land_1_LushFernAndHeather.fbx (林下羽状蕨类与海岸灌木花丛 - 15片舒展蕨叶+灌木底簇)
    # =================================================================
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    col = bm.loops.layers.color.new("Col")
    for i in range(15):
        ang = (i / 15.0) * math.pi * 2.0 + (i % 2) * 0.2
        pitch = 0.45 + (i % 3) * 0.22
        length = 0.75 + (i % 4) * 0.18
        add_fern_frond(bm, uv, col, (math.cos(ang) * 0.08, 0.02, math.sin(ang) * 0.08), ang, pitch, length, 0.16, (0.28, 0.62, 0.26))
    for i in range(4):
        a = i*math.tau/4
        add_organic_mound(bm,uv,col,(math.cos(a)*.16,.13,math.sin(a)*.16),
                          (.23,.16,.21),1,i,(.31,.46,.29))
        add_organic_mound(bm,uv,col,(math.cos(a)*.23,.26,math.sin(a)*.23),
                          (.087,.055,.075),1,i+.5,(.62,.44,.52))
    bake_vertex_ao_and_export(bm, "Land_1_LushFernAndHeather", os.path.join(models_dir, "Land_1_LushFernAndHeather.fbx"))

    # =================================================================
    # 3. Land_2_WildflowerGrassTussock.fbx (高密度海滨野草与雏菊花丛 - 28株错落草叶)
    # =================================================================
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    col = bm.loops.layers.color.new("Col")
    for i in range(28):
        ang = i * 2.39996
        rad = 0.06 + (i / 28.0) * 0.68
        ox = math.cos(ang) * rad
        oz = math.sin(ang) * rad
        h = 0.42 + (i % 5) * 0.15
        add_fern_frond(bm, uv, col, (ox, -0.02, oz), ang + 0.4, 0.85, h, 0.055, (.38+(i%3)*.036,.53+(i%4)*.023,.28),False)
    for i in range(6):
        a = i*2.39996
        add_daisy(bm,uv,col,math.cos(a)*.46,math.sin(a)*.46,.38+(i%4)*.07,i)
    bake_vertex_ao_and_export(bm, "Land_2_WildflowerGrassTussock", os.path.join(models_dir, "Land_2_WildflowerGrassTussock.fbx"))

    # =================================================================
    # 4. Land_3_MossyBoulderCluster.fbx (覆苔天然风化母子岩簇)
    # =================================================================
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    col = bm.loops.layers.color.new("Col")
    rocks_cfg = [
        ((0.0, 0.32, 0.0), (0.95, 0.62, 0.82), 1.2),
        ((0.78, 0.18, 0.42), (0.58, 0.38, 0.52), 2.5),
        ((-0.68, 0.15, 0.35), (0.52, 0.34, 0.48), 3.8),
        ((0.25, 0.12, -0.68), (0.46, 0.28, 0.42), 5.1),
    ]
    for center, rads, seed in rocks_cfg:
        add_organic_mound(bm, uv, col, center, rads, subdiv=2, seed=seed, rgb=(.67,.69,.63), role=2)
    bake_vertex_ao_and_export(bm, "Land_3_MossyBoulderCluster", os.path.join(models_dir, "Land_3_MossyBoulderCluster.fbx"))

    # =================================================================
    # 5. Prop_0_DryingNetAndRack.fbx (海湾晒网木架与自然垂坠弧形渔网 + 酒桶)
    # =================================================================
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    col = bm.loops.layers.color.new("Col")
    # 左右 A 字形原木支架 + 顶部横梁
    for side in (-1.6, 1.6):
        add_curved_tube(bm, uv, col, [(side, -0.1, -0.65), (side, 2.25, 0.0)], [0.09, 0.07], 7, (0.65, 0.52, 0.38))
        add_curved_tube(bm, uv, col, [(side, -0.1, 0.65), (side, 2.25, 0.0)], [0.09, 0.07], 7, (0.65, 0.52, 0.38))
    add_curved_tube(bm, uv, col, [(-1.85, 2.18, 0.0), (0.0, 2.22, 0.0), (1.85, 2.18, 0.0)], [0.08, 0.085, 0.08], 8, (0.68, 0.55, 0.40))
    # 垂挂的弧形晒网与底部坠石木桶
    for nx in range(15):
        x = -1.4 + (nx / 14.0) * 2.8
        sag = math.sin((nx / 14.0) * math.pi) * 0.25
        add_curved_tube(bm, uv, col, [(x, 2.18, 0.0), (x, 1.25 - sag * 0.5, 0.18 + sag * 0.4), (x, 0.45 - sag, 0.08)], [0.018, 0.018, 0.018], 5, (0.91, 0.82, 0.67), 3)
    for ny in (1.94, 1.68, 1.42, 1.16, .90, .64):
        add_curved_tube(bm, uv, col, [(-1.4, ny, 0.08), (0.0, ny - 0.16, 0.24), (1.4, ny, 0.08)], [0.016, 0.016, 0.016], 5, (0.91, 0.82, 0.67), 3)
    # 架旁配套橡木桶与晒鱼木箱
    add_curved_tube(bm, uv, col, [(1.25, 0.0, 0.55), (1.25, 0.38, 0.55), (1.25, 0.76, 0.55)], [0.26, 0.30, 0.26], 10, (0.58, 0.44, 0.30))
    add_beveled_box(bm, uv, col, (-1.15, 0.22, 0.48), (0.68, 0.44, 0.52), (0, 0.25, 0), 0.025, (0.66, 0.54, 0.38))
    bake_vertex_ao_and_export(bm, "Prop_0_DryingNetAndRack", os.path.join(models_dir, "Prop_0_DryingNetAndRack.fbx"))

    # =================================================================
    # 6. Prop_1_WoodenHandcartAndBarrels.fbx (渔村双轮木推车与货物堆)
    # =================================================================
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    col = bm.loops.layers.color.new("Col")
    # 车斗底板与四面栏板
    add_beveled_box(bm, uv, col, (0.0, 0.48, 0.0), (1.15, 0.08, 1.55), (0.08, 0, 0), 0.02, (0.68, 0.55, 0.40))
    for sx in (-0.54, 0.54):
        add_beveled_box(bm, uv, col, (sx, 0.72, 0.0), (0.07, 0.42, 1.52), (0.08, 0, 0), 0.015, (0.64, 0.50, 0.36))
        # 两根向前延伸的推车把手
        add_curved_tube(bm, uv, col, [(sx, 0.48, -0.75), (sx, 0.42, 0.85), (sx * 0.85, 0.34, 1.55)], [0.045, 0.045, 0.038], 6, (0.55, 0.42, 0.28))
        # 两侧大木轮 (用 12 段圆环构成真实车轮 + 十字辐条)
        w_pts = []
        w_rads = []
        for s in range(16):
            a = (s / 16.0) * math.pi * 2.0
            w_pts.append((sx * 1.16, 0.42 + math.sin(a) * 0.38, -0.10 + math.cos(a) * 0.38))
            w_rads.append(0.042)
        add_curved_tube(bm, uv, col, w_pts, w_rads, 6, (0.70, 0.61, 0.48), 4, closed=True)
        add_beveled_box(bm, uv, col, (sx * 1.16, 0.42, -0.10), (0.05, 0.74, 0.06), (0, 0, 0), 0.01, (0.52, 0.40, 0.28))
        add_beveled_box(bm, uv, col, (sx * 1.16, 0.42, -0.10), (0.05, 0.06, 0.74), (0, 0, 0), 0.01, (0.52, 0.40, 0.28))
    # 车斗内满载的货箱与圆木桶
    add_beveled_box(bm, uv, col, (-0.18, 0.76, -0.22), (0.58, 0.46, 0.56), (0.08, 0.15, 0), 0.02, (0.76, 0.64, 0.46))
    add_curved_tube(bm, uv, col, [(0.22, 0.52, 0.28), (0.22, 1.05, 0.24)], [0.22, 0.22], 10, (0.60, 0.46, 0.32))
    bake_vertex_ao_and_export(bm, "Prop_1_WoodenHandcartAndBarrels", os.path.join(models_dir, "Prop_1_WoodenHandcartAndBarrels.fbx"))

    # =================================================================
    # 7. Prop_2_CampfireAndLogSeats.fbx (海岸鹅卵石篝火营地、三脚吊锅架与原木长凳)
    # =================================================================
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    col = bm.loops.layers.color.new("Col")
    # 10 块围成一圈的天然海石火塘
    for i in range(10):
        a = (i / 10.0) * math.pi * 2.0
        add_organic_mound(bm, uv, col, (math.cos(a) * 0.62, 0.12, math.sin(a) * 0.62), (0.22, 0.16, 0.20), subdiv=1, seed=i * 1.7, rgb=(.65,.66,.58), role=2)
    # 交叉堆叠的篝火木柴
    for i in range(5):
        a = (i / 5.0) * math.pi * 2.0
        add_curved_tube(bm, uv, col, [(math.cos(a) * 0.45, 0.05, math.sin(a) * 0.45), (-math.cos(a) * 0.12, 0.42, -math.sin(a) * 0.12)], [0.065, 0.045], 6, (0.42, 0.30, 0.20))
    # 三脚烹饪木架
    for i in range(3):
        a = (i / 3.0) * math.pi * 2.0 + 0.3
        add_curved_tube(bm, uv, col, [(math.cos(a) * 0.95, -0.05, math.sin(a) * 0.95), (0.0, 1.65, 0.0)], [0.05, 0.035], 6, (0.55, 0.42, 0.28))
    # 周围两根供渔民歇脚的原木长凳
    add_curved_tube(bm, uv, col, [(-1.45, 0.24, -1.35), (1.35, 0.24, -1.45)], [0.22, 0.20], 9, (0.62, 0.48, 0.34))
    add_curved_tube(bm, uv, col, [(-1.55, 0.24, 1.15), (-1.25, 0.24, -0.85)], [0.21, 0.19], 9, (0.62, 0.48, 0.34))
    add_curved_tube(bm,uv,col,[(0,1.61,0),(0,.83,0)],[.018,.016],5,(.66,.67,.62),4)
    add_organic_mound(bm,uv,col,(0,.66,0),(.22,.18,.22),2,3,(.45,.45,.42),4)
    for i in range(5):
        a=i*math.tau/5
        x,z=math.cos(a)*.13,math.sin(a)*.13
        add_curved_tube(bm,uv,col,[(x,.20,z),(x*.68,.42,z*.68),(x*.35+.045,.57+(i%3)*.06,z*.35)],
                        [.098,.062,.007],6,(1,.52+.07*(i%3),.12),5)
    bake_vertex_ao_and_export(bm, "Prop_2_CampfireAndLogSeats", os.path.join(models_dir, "Prop_2_CampfireAndLogSeats.fbx"))

    # =================================================================
    # 8. Prop_3_RusticFenceSegment.fbx (海岬村落原木交叉围栏与石底座段 - 3.2m 宽)
    # =================================================================
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    col = bm.loops.layers.color.new("Col")
    for px in (-1.5, 0.0, 1.5):
        # 根部压脚海石 + 削尖原木桩
        add_organic_mound(bm, uv, col, (px, 0.12, 0.0), (0.28, 0.18, 0.26), subdiv=1, seed=px + 5.0, rgb=(.66,.67,.62), role=2)
        add_curved_tube(bm, uv, col, [(px, -0.15, 0.0), (px, 0.92, 0.0), (px, 1.08, 0.0)], [0.10, 0.09, 0.02], 7, (0.58, 0.45, 0.32))
    # 上下两道自然微弯的横木栏杆
    add_curved_tube(bm, uv, col, [(-1.62, 0.76, 0.06), (0.0, 0.73, 0.08), (1.62, 0.77, 0.06)], [0.055, 0.055, 0.055], 6, (0.66, 0.53, 0.38))
    add_curved_tube(bm, uv, col, [(-1.62, 0.42, -0.06), (0.0, 0.45, -0.08), (1.62, 0.41, -0.06)], [0.055, 0.055, 0.055], 6, (0.66, 0.53, 0.38))
    bake_vertex_ao_and_export(bm, "Prop_3_RusticFenceSegment", os.path.join(models_dir, "Prop_3_RusticFenceSegment.fbx"))

    # =================================================================
    # 9. Prop_4_BeachedRowboatAndOars.fbx (岸滩斜靠修缮小木船、弧形船肋与船桨)
    # =================================================================
    clear_scene()
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    col = bm.loops.layers.color.new("Col")
    # 用 9 组船体横截面构建优雅的流线型尖头木船壳
    hull_z = [-1.8, -1.4, -0.8, -0.2, 0.4, 1.0, 1.45, 1.75, 1.92]
    hull_w = [0.42, 0.68, 0.85, 0.88, 0.82, 0.68, 0.45, 0.18, 0.02]
    hull_h = [0.48, 0.44, 0.42, 0.42, 0.44, 0.46, 0.50, 0.54, 0.58]
    hull_rings = []
    for z, w, h in zip(hull_z, hull_w, hull_h):
        r = [
            bm.verts.new((-w, h, z)),
            bm.verts.new((-w * 0.72, 0.06, z)),
            bm.verts.new((0.0, 0.0, z)),
            bm.verts.new((w * 0.72, 0.06, z)),
            bm.verts.new((w, h, z)),
        ]
        hull_rings.append(r)
    for i in range(len(hull_rings) - 1):
        r0, r1 = hull_rings[i], hull_rings[i + 1]
        for s in range(4):
            f = bm.faces.new((r0[s], r0[s + 1], r1[s + 1], r1[s]))
            f.material_index = 1
            f.smooth = True
            inner = bm.faces.new(tuple(bm.verts.new(v.co+Vector((0,.025,0))) for v in reversed(f.verts)))
            inner.material_index = 1
            for loop in inner.loops:
                loop[uv].uv = (loop.vert.co.z,loop.vert.co.x)
                loop[col] = (.78,.70,.57,.80)
            for loop in f.loops:
                loop[uv].uv = (s * 0.25, i * 0.15)
                loop[col] = (0.72, 0.58, 0.42, 0.75)
    for i in range(1,len(hull_rings)-2):
        ring = hull_rings[i]
        pts = [tuple(v.co+Vector((0,.035,0))) for v in ring]
        add_curved_tube(bm,uv,col,pts,[.030]*5,5,(.77,.67,.54),1)
    add_beveled_box(bm,uv,col,(0,.23,hull_z[0]),(.80,.43,.068),bevel=.019)
    # 船舱内 3 条横座板
    for bz, bw in ((-0.8, 0.78), (0.0, 0.82), (0.8, 0.68)):
        add_beveled_box(bm, uv, col, (0.0, 0.34, bz), (bw * 1.85, 0.06, 0.26), (0, 0, 0), 0.015, (0.64, 0.50, 0.36))
    # 斜靠在船边的长木桨
    add_curved_tube(bm, uv, col, [(0.55, 0.45, -0.4), (1.15, 0.18, 0.4), (1.45, 0.05, 0.8)], [0.03, 0.03, 0.08], 6, (0.76, 0.64, 0.46))
    bake_vertex_ao_and_export(bm, "Prop_4_BeachedRowboatAndOars", os.path.join(models_dir, "Prop_4_BeachedRowboatAndOars.fbx"))


def main():
    global source_dir, role_dir
    parser = argparse.ArgumentParser()
    parser.add_argument("--project",type=Path,default=Path.cwd())
    parser.add_argument("--out-dir",type=Path)
    args = parser.parse_args(sys.argv[sys.argv.index("--")+1:] if "--" in sys.argv else [])
    project = args.project.resolve()
    output = args.out_dir.resolve() if args.out_dir else project/"Assets/Deepsea/Resources/SeaLushWorldV33"
    source_dir = project/"Tools/SeaLushWorldV33/SourceMeshes"
    role_dir = output/"Roles"
    models = output/"Models"
    models.mkdir(parents=True,exist_ok=True)
    generate_all_v33_assets(models)
    if len(MANIFEST) != 10:
        raise RuntimeError("Expected ten authored models")
    (source_dir.parent/"model-manifest.json").write_text(json.dumps({"models":MANIFEST},indent=2)+"\n",
                                                       encoding="utf-8")
    print("LUSH_V33_MESHES",len(MANIFEST),sum(m["triangles"] for m in MANIFEST))


if __name__ == "__main__":
    main()

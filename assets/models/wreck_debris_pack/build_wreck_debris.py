from pathlib import Path
import numpy as np, json, struct, math, zipfile
from scipy.spatial import ConvexHull
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d.art3d import Poly3DCollection
P=Path(__file__).parent
mats=[('Scorched armor',[.25,.29,.30,1],.6,.72),('Exposed metal',[.40,.43,.42,1],.75,.48),('Dark interior',[.065,.08,.09,1],.25,.9),('Faded ochre',[.48,.29,.10,1],.3,.8)]
parts=[]
def hull(name,pts,mat):
 pts=np.array(pts,float);h=ConvexHull(pts);faces=h.simplices.copy()
 for i,f in enumerate(faces):
  if np.dot(np.cross(pts[f[1]]-pts[f[0]],pts[f[2]]-pts[f[0]]),h.equations[i,:3])<0:faces[i]=f[::-1]
 parts.append((name,pts,faces,mat))
def box(name,c,s,mat=0,b=.025):
 c=np.array(c);s=np.array(s)/2;b=min(b,min(s)*.7);pts=[]
 for axis in range(3):
  for sign in [-1,1]:
   for a in [-1,1]:
    for d in [-1,1]:
     p=np.zeros(3);p[axis]=sign*s[axis];p[(axis+1)%3]=a*(s[(axis+1)%3]-b);p[(axis+2)%3]=d*(s[(axis+2)%3]-b);pts.append(c+p)
 hull(name,pts,mat)
def beam(name,a,b,w=.09,mat=1):
 a=np.array(a);b=np.array(b);v=b-a;v/=np.linalg.norm(v);u=np.cross(v,[0,1,0] if abs(v[1])<.9 else [1,0,0]);u/=np.linalg.norm(u);t=np.cross(v,u)
 hull(name,[p+u*i*w/2+t*j*w/2 for p in [a,b] for i in [-1,1] for j in [-1,1]],mat)
def plate(name,poly,y,th=.09,mat=0):
 # convex shards only; irregular boundaries, real thickness
 hull(name,[(x,y+dy,z) for x,z in poly for dy in [-th/2,th/2]],mat)
def ring(name,z,r1,r2,depth,mat=1,segments=12,start=0,end=None):
 end=segments if end is None else end
 for i in range(start,end):
  a=i*2*math.pi/segments;b=(i+1)*2*math.pi/segments
  hull(name,[(r*math.cos(t),r*math.sin(t),zz) for r in [r1,r2] for t in [a,b] for zz in [z-depth/2,z+depth/2]],mat)
kit={}
def keep(name):
 global parts
 kit[name]=parts;parts=[]
# 1 broad hull shard with exposed rim, scratches, one missing corner
poly=[(-1,-.62),(.62,-.73),(1.02,-.18),(.55,.65),(-.65,.81),(-1.12,.17)]
plate('Backing',poly,0,.11,1);plate('Armor inset',[(x*.93,z*.92) for x,z in poly],.09,.12)
box('Paint stripe',(-.3,.16,.12),(.17,.018,1.1),3,.003)
for x,z,l in [(.2,-.1,.5),(.42,.22,.31),(-.68,-.2,.23)]:box('Scorch gouge',(x,.158,z),(.035,.018,l),2,.003)
beam('Torn support',(-.8,-.14,-.4),(.55,-.14,.4),.12,2)
keep('hull_panel')
# 2 bent panel, three facets with lifted torn ends
plate('Panel center',[(-.5,-.85),(.5,-.74),(.6,.62),(-.45,.9)],0,.12)
hull('Bent wing',[(-.5,-.06,-.85),(-.5,.06,-.85),(-.45,-.06,.9),(-.45,.06,.9),(-1.05,.38,-.5),(-1.05,.5,-.5),(-.96,.3,.6),(-.96,.42,.6)],0)
hull('Torn upturn',[(.5,-.06,-.74),(.5,.06,-.74),(.6,-.06,.62),(.6,.06,.62),(1.02,.35,-.4),(.89,.49,.2),(.82,.28,.61)],1)
for z in [-.5,0,.5]:box('Panel ribs',(0,-.1,z),(.82,.12,.08),2)
box('Warning paint',(0,.07,-.22),(.18,.02,.7),3,.004)
keep('bent_armor')
# 3 open half-cylinder fuselage, intentionally broken rim and beams
ring('Rear bulkhead',.72,.67,.80,.16,1,12,0,8)
ring('Front broken bulkhead',-.62,.67,.80,.12,1,12,0,7)
for i in range(8):
 a=i*2*math.pi/12;b=(i+1)*2*math.pi/12;front=-.95+[.1,.32,-.15,.2,-.05,.4,.08,.3][i]
 hull('Hull strip',[(r*math.cos(t),r*math.sin(t),z) for r in [.77,.85] for t in [a,b] for z in [front,.85]],0)
for i in [0,2,4,6]:
 a=i*math.pi/6;beam('Interior stringer',(.65*math.cos(a),.65*math.sin(a),-.94),(.65*math.cos(a),.65*math.sin(a),.86),.09,2)
beam('Dangling strut',(-.7,0,-.55),(-.92,-.35,-1.2),.07)
keep('broken_fuselage')
# 4 open engine nozzle and fractured casing; no end cap
ring('Nozzle lip',-.85,.48,.59,.14,1)
for i in range(12):
 a=i*math.pi/6;b=(i+1)*math.pi/6
 pts=[]
 for z,r in [(-.8,.56),(.1,.34)]:
  for rr in [r-.07,r]:
   for t in [a,b]:pts.append((rr*math.cos(t),rr*math.sin(t),z))
 hull('Nozzle wall',pts,2)
ring('Engine housing',.2,.26,.49,.34,0)
ring('Broken casing',.52,.29,.48,.3,0,12,1,10)
for i in range(8):
 a=i*math.pi/4;beam('Cooling rail',(.53*math.cos(a),.53*math.sin(a),-.57),(.44*math.cos(a),.44*math.sin(a),.4),.065,1)
beam('Fractured feed',(.2,.2,.52),(.36,.37,.97),.12,1)
keep('engine_fragment')
# 5 skeletal truss with broken diagonal and a little remaining armor
for x in [-.43,.43]:
 for y in [-.28,.28]:beam('Longeron',(x,y,-1.05),(x,y,.95),.09,1)
for z in [-.9,0,.85]:
 for y in [-.28,.28]:beam('Cross brace',(-.43,y,z),(.43,y,z),.07,2)
 for x in [-.43,.43]:beam('Vertical brace',(x,-.28,z),(x,.28,z),.07,1)
beam('Diagonal',(-.43,.28,-.9),(.43,.28,0),.065)
beam('Broken diagonal',(-.43,.28,0),(.08,.43,.46),.065)
plate('Remaining skin',[(-.49,-.92),(.46,-.91),(.44,-.44),(-.35,-.2)],.35,.07)
keep('exposed_truss')
# 6 severed angular prow chunk, dark recessed end plus ragged exposed rods
hull('Prow armor',[(-.64,-.25,.45),(.64,-.25,.45),(-.6,.28,.45),(.6,.28,.45),(-.25,-.14,-1),(.25,-.14,-1),(0,.12,-1.18)],0)
box('Severed interior',(0,0,.47),(1.02,.38,.05),2,.03)
for x,y,l in [(-.4,.1,.48),(.3,-.1,.34),(0,.13,.22)]:beam('Broken internal rib',(x,y,.44),(x+.05,y+.07,.44+l),.075,1)
plate('Upper armor patch',[(-.42,.3),(.42,.3),(.2,-.45),(-.2,-.55)],.22,.045,1)
box('Faded paint',(0,.251,.12),(.15,.015,.25),3,.004)
keep('severed_prow')
def export(name,parts):
 blob=bytearray();views=[];access=[];prims=[]
 def acc(a):
  a=np.asarray(a,dtype='<f4');off=len(blob);blob.extend(a.tobytes());views.append({'buffer':0,'byteOffset':off,'byteLength':a.nbytes,'target':34962});access.append({'bufferView':len(views)-1,'componentType':5126,'count':len(a),'type':'VEC3','min':a.min(0).tolist(),'max':a.max(0).tolist()});return len(access)-1
 for mi in range(len(mats)):
  vs=[];ns=[]
  for _,v,f,m in parts:
   if m!=mi:continue
   for face in f:
    tri=v[face];n=np.cross(tri[1]-tri[0],tri[2]-tri[0]);n/=np.linalg.norm(n);vs.extend(tri);ns.extend([n]*3)
  if vs:prims.append({'attributes':{'POSITION':acc(vs),'NORMAL':acc(ns)},'material':mi})
 g={'asset':{'version':'2.0','generator':'Grim Space debris kit'},'scene':0,'scenes':[{'nodes':[0]}],'nodes':[{'name':name,'mesh':0}],'meshes':[{'name':name,'primitives':prims}],'materials':[{'name':n,'pbrMetallicRoughness':{'baseColorFactor':c,'metallicFactor':m,'roughnessFactor':r}} for n,c,m,r in mats],'buffers':[{'byteLength':len(blob)}],'bufferViews':views,'accessors':access}
 j=json.dumps(g,separators=(',',':')).encode();j+=b' '*((-len(j))%4);blob+=b'\0'*((-len(blob))%4);b=struct.pack('<4sII',b'glTF',2,28+len(j)+len(blob))+struct.pack('<I4s',len(j),b'JSON')+j+struct.pack('<I4s',len(blob),b'BIN\0')+blob;(P/(name+'.glb')).write_bytes(b)
 assert struct.unpack_from('<I',b,8)[0]==len(b)
 for v in views:assert v['byteOffset']+v['byteLength']<=len(blob)
 return sum(len(f) for _,_,f,_ in parts)
counts={n:export(n,p) for n,p in kit.items()}
fig=plt.figure(figsize=(13,9),facecolor='#192129')
for k,(name,ps) in enumerate(kit.items()):
 ax=fig.add_subplot(2,3,k+1,projection='3d');ax.set_facecolor('#192129')
 for _,v,f,m in ps:
  q=v[:,[0,2,1]];tris=q[f];norm=np.cross(tris[:,1]-tris[:,0],tris[:,2]-tris[:,0]);norm/=np.linalg.norm(norm,axis=1)[:,None];shade=.48+.52*np.abs(norm@np.array([.35,-.5,.79]));col=np.clip(np.array(mats[m][1][:3])*shade[:,None]*1.85,0,1)
  ax.add_collection3d(Poly3DCollection(tris,facecolors=col,edgecolors='none'))
 ax.set(xlim=(-1.2,1.2),ylim=(-1.2,1.2),zlim=(-1.2,1.2));ax.set_box_aspect((1,1,1));ax.view_init(elev=29,azim=-62);ax.set_proj_type('ortho');ax.set_axis_off();ax.set_title(name.replace('_',' ').upper()+'  /  '+str(counts[name])+' tris',color='#bac6cb',fontsize=10,pad=-8)
fig.text(.045,.95,'GRIM SPACE / MODULAR WRECK DEBRIS',fontsize=22,color='#e2e7e9',weight='bold');fig.text(.045,.912,'Six reusable fragments • actual mesh previews • no external textures',fontsize=11,color='#a2afb8')
plt.subplots_adjust(left=.015,right=.985,top=.865,bottom=.02,wspace=-.15,hspace=-.12);plt.savefig(P/'wreck_debris_preview.png',dpi=130);plt.close()
(P/'README.md').write_text('''# Grim Space modular wreck debris\n\nSix original procedural GLBs. No third-party asset data or textures.\n\nImport individual GLBs into Godot, place them under a Node3D, then rotate, scale and overlap to compose wrecks. Each file contains one mesh with material surfaces. Shared material definitions use scorched gray armor, exposed steel, dark interior, faded ochre paint. No animation, collision, lights or external texture dependencies.\n\nY is up; Z is the length axis. Pivots are close to fragment centers for easy random rotation, not attachment sockets. Pieces are roughly 1–2.4 arbitrary units; scale to your ships. No need for vertices to align to gameplay's integer grid: keep logical wreck placement separate from visual offsets.\n\nSuggested compositions:\n- Small wreck: broken_fuselage + engine_fragment behind it, two hull panels nearby.\n- Skeletal wreck: exposed_truss + severed_prow at one end + bent_armor partially overlapping.\n- Debris field: duplicate panels and engine pieces, vary rotations and sizes; avoid perfectly even spacing.\n\nThese are simple prototypes, with geometry detail and flat PBR materials rather than authored wear textures. The preview uses basic shading; Godot lighting will differ. GLB headers/buffer bounds checked; not tested in the game.\n\nTriangle counts:\n'''+''.join(f'- {n}: {c}\n' for n,c in counts.items())+'\nRebuild with Python, numpy, scipy and matplotlib: python build_wreck_debris.py\n')
with zipfile.ZipFile(P/'wreck_debris_pack.zip','w',zipfile.ZIP_DEFLATED) as z:
 for f in sorted(P.iterdir()):
  if f.suffix in ['.glb','.png','.md','.py']:z.write(f,f.name)
print(counts)

"""Measure opaque connected components; write crop metadata, never edit art."""
from pathlib import Path
from PIL import Image
import numpy as np
from scipy.ndimage import label,find_objects
root=Path(__file__).resolve().parents[1]/'assets/v0519'
rows=['# sheet,index,x,y,width,height']
for p in sorted(root.glob('*-?.png')):
 a=np.array(Image.open(p));lab,_=label(a[:,:,3]>180);sizes=np.bincount(lab.ravel());ids=np.argsort(sizes[1:])[-16:]+1;objects=find_objects(lab)
 boxes=[]
 for i in ids:
  if sizes[i]<1000:raise ValueError('Missing character '+p.name)
  y,x=objects[i-1];boxes.append([x.start,y.start,x.stop,y.stop])
 if len(boxes)!=16:
  # Some edited sheets have touching hair tips. Split their authored 4x4
  # layout before measuring; never silently emit fewer than 16 frames.
  boxes=[]
  for rr in range(4):
   for cc in range(4):
    y0=round(rr*a.shape[0]/4);y1=round((rr+1)*a.shape[0]/4)
    x0=round(cc*a.shape[1]/4);x1=round((cc+1)*a.shape[1]/4)
    yy,xx=np.where(a[y0:y1,x0:x1,3]>180)
    if len(xx)<1000:raise ValueError('Missing cell '+p.name)
    boxes.append([x0+int(xx.min()),y0+int(yy.min()),x0+int(xx.max())+1,y0+int(yy.max())+1])
 boxes.sort(key=lambda b:(b[1]+b[3])/2)
 ordered=[]
 for r in range(4):ordered.extend(sorted(boxes[r*4:r*4+4],key=lambda b:b[0]))
 for i,(x,y,r,b) in enumerate(ordered):
  x=max(0,x-1);y=max(0,y-1);r=min(a.shape[1],r+1);b=min(a.shape[0],b+1)
  rows.append(f'{p.name},{i},{x},{y},{r-x},{b-y}')
(root/'frames.csv').write_text('\n'.join(rows)+'\n')
print('Measured',len(rows)-1,'complete sprite rectangles')

"""Build a self-contained authored-frame review; no runtime emulation."""
from pathlib import Path
import json,base64
root=Path(__file__).resolve().parents[1]
frames={}
for line in (root/'assets/v0519/frames.csv').read_text().splitlines():
 if line.startswith('#'):continue
 name,i,*rect=line.split(',');frames.setdefault(name,[]).append(list(map(int,rect)))
images={n:'data:image/png;base64,'+base64.b64encode((root/'assets/v0519'/n).read_bytes()).decode() for n in frames}
html='''<!doctype html><meta charset="utf-8"><title>0.5.19 素材检查</title><style>body{background:#25262e;color:#eee;font:18px sans-serif;margin:32px}canvas{background:#656776;display:block;margin:20px 0}button,select,input{font:inherit;margin:8px}p{max-width:800px}</style><h1>0.5.19 袖子修正版 · 原始帧检查</h1><p>逐帧检查长袖、头发与姿势。此页展示最新素材，不模拟桌面程序的转身插值。运行 tests/Run-Animation-Check.cmd 可生成实际渲染器的动画预览。</p><select id="sheet"></select><button id="prev">上一帧</button><button id="next">下一帧</button><input id="frame" type="range" min="0" max="15" value="0"><span id="label"></span><canvas id="view" width="600" height="640"></canvas><script>const RECT=RECT_DATA, SOURCES=SOURCE_DATA;const sheet=document.getElementById('sheet'),frame=document.getElementById('frame'),ctx=document.getElementById('view').getContext('2d');let im=new Image();Object.keys(RECT).forEach(n=>sheet.add(new Option(n,n)));function draw(){if(!im.complete)return;const r=RECT[sheet.value][+frame.value];ctx.clearRect(0,0,600,640);const scale=Math.min(550/r[2],590/r[3]);ctx.drawImage(im,...r,(600-r[2]*scale)/2,620-r[3]*scale,r[2]*scale,r[3]*scale);document.getElementById('label').textContent=(+frame.value+1)+'/16'}function load(){im=new Image();im.onload=draw;im.src=SOURCES[sheet.value]}sheet.onchange=load;frame.oninput=draw;document.getElementById('prev').onclick=()=>{frame.value=Math.max(0,+frame.value-1);draw()};document.getElementById('next').onclick=()=>{frame.value=Math.min(15,+frame.value+1);draw()};load();</script>'''
(root/'Animation-Preview.html').write_text(html.replace('RECT_DATA',json.dumps(frames)).replace('SOURCE_DATA',json.dumps(images)),encoding='utf-8')

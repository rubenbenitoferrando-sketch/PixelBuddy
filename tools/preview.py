import sys, json
from pathlib import Path
from PIL import Image, ImageDraw
out = Path(sys.argv[1]); S = 8
AV = out/"avatars/ninja"
man = json.loads((AV/"manifest.json").read_text())
states = ["idle","run","katana","train","sleep","alert","ask"]
BG=(232,236,242,255)
def frames(var, st):
    sh = Image.open(AV/f"ninja_{var}_{st}.png")
    fw = man["states"][st]["frameSize"][0]; ax = man["states"][st]["anchorX"]
    n = sh.width//fw
    fr = [sh.crop((i*fw,0,i*fw+fw,32)) for i in range(n)]
    # normaliza a 56x32 con el ninja centrado en x=32
    res=[]
    for f in fr:
        c=Image.new("RGBA",(56,32),(0,0,0,0)); c.alpha_composite(f,(32-ax,0)); res.append(c)
    return res
# hoja de contacto: variante negra, todos los frames
rows=[]
maxf=max(man["states"][s]["frames"] for s in states)
sheet=Image.new("RGBA",(120+maxf*56*S, len(states)*32*S),BG)
d=ImageDraw.Draw(sheet)
for r,st in enumerate(states):
    d.text((10, r*32*S+120), st.upper(), fill=(30,30,40,255))
    for i,f in enumerate(frames("azul",st)):
        sheet.alpha_composite(f.resize((56*S,32*S),Image.NEAREST),(120+i*56*S, r*32*S))
sheet.save(out/"preview_frames.png")
# variantes de color (idle frame 0 + katana frame 3)
v=man["variants"]; S2=6
vs=Image.new("RGBA",(len(v)*56*S2, 4*32*S2),BG)
for i,name in enumerate(v):
    vs.alpha_composite(frames(name,"katana")[4].resize((56*S2,32*S2),Image.NEAREST),(i*56*S2,0))
    vs.alpha_composite(frames(name,"run")[1].resize((56*S2,32*S2),Image.NEAREST),(i*56*S2,32*S2))
    vs.alpha_composite(frames(name,"alert")[0].resize((56*S2,32*S2),Image.NEAREST),(i*56*S2,2*32*S2))
    vs.alpha_composite(frames(name,"ask")[0].resize((56*S2,32*S2),Image.NEAREST),(i*56*S2,3*32*S2))
vs.save(out/"preview_colores.png")
# GIF animado: todos los estados en fila
S3=5; seq=[]
for t in range(48):
    im=Image.new("RGBA",(len(states)*56*S3,32*S3),BG)
    for c,st in enumerate(states):
        fr=frames("azul",st); info=man["states"][st]
        k=int(t*info["fps"]/12)
        lf=info["loopFrom"]
        idx = k if k < len(fr) else lf + (k-lf)%(len(fr)-lf)
        im.alpha_composite(fr[idx].resize((56*S3,32*S3),Image.NEAREST),(c*56*S3,0))
    seq.append(im.convert("RGB"))
seq[0].save(out/"preview_animaciones.gif",save_all=True,append_images=seq[1:],duration=83,loop=0)

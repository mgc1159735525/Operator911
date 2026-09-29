"""Reproducible original UI geometry; no third-party game art used."""
from pathlib import Path
from PIL import Image, ImageDraw
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Minglu/Resources/Minglu/UI/Noir'
OUT.mkdir(parents=True,exist_ok=True)
SOURCE=ROOT/'Tools/UI/Sources'
SOURCE.mkdir(parents=True,exist_ok=True)
def shape(name,fill,stroke,cut=6):
    im=Image.new('RGBA',(128,64));d=ImageDraw.Draw(im)
    pts=[(0,0),(127-cut,0),(127,cut),(127,63),(cut,63),(0,63-cut)]
    d.polygon(pts,fill=fill);d.line(pts+[pts[0]],fill=stroke,width=2)
    im.save(OUT/(name+'.png'))
    (SOURCE/(name+'.svg')).write_text(f'<svg xmlns="http://www.w3.org/2000/svg" width="128" height="64" viewBox="0 0 128 64"><path d="M0 0 H{127-cut} L127 {cut} V63 H{cut} L0 {63-cut} Z" fill="rgb{fill[:3]}" stroke="rgb{stroke[:3]}" stroke-width="2"/></svg>')
for name,fill,edge in [('button_idle',(230,230,230,255),(255,255,255,255)),('button_hover',(255,255,255,255),(255,255,255,255)),('button_pressed',(175,175,175,255),(230,230,230,255)),('button_disabled',(160,160,160,255),(180,180,180,255)),('panel',(240,240,240,255),(255,255,255,255)),('card',(235,235,235,255),(255,255,255,255)),('dialog',(247,247,247,255),(255,255,255,255))]:shape(name,fill,edge)
icons={
 'tasks': [('rect',(15,12,49,53)),('line',(23,24,41,24)),('line',(23,33,41,33)),('line',(23,42,37,42))],
 'routes':[('ellipse',(5,23,21,39)),('ellipse',(43,23,59,39)),('line',(21,31,43,31))],
 'people':[('ellipse',(24,8,40,24)),('line',(14,53,14,40,24,31,40,31,50,40,50,53))],
 'map':[('line',(8,14,24,8,40,14,56,8,56,50,40,56,24,50,8,56,8,14)),('line',(24,8,24,50)),('line',(40,14,40,56))],
 'report':[('line',(13,9,45,9,53,17,53,55,13,55,13,9)),('line',(22,25,43,25)),('line',(22,34,43,34)),('line',(22,43,37,43))],
 'alliance':[('line',(7,32,20,19,32,29,44,19,57,32,44,45,32,37,20,45,7,32))],
 'intel':[('rect',(7,16,57,49)),('line',(7,16,32,36,57,16))],
 'money':[('ellipse',(9,9,55,55)),('rect',(25,25,39,39))],
 'risk':[('line',(32,6,59,55,5,55,32,6)),('line',(32,23,32,38)),('ellipse',(30,44,34,48))],
 'clock':[('ellipse',(8,8,56,56)),('line',(32,16,32,33,43,40))],
 'save':[('rect',(11,9,53,55)),('rect',(21,9,43,25)),('rect',(20,36,44,55))],
 'settings':[('line',(8,18,56,18)),('line',(8,32,56,32)),('line',(8,46,56,46)),('ellipse',(18,13,28,23)),('ellipse',(37,27,47,37)),('ellipse',(20,41,30,51))],
 'arrow':[('line',(12,32,52,32)),('line',(35,15,52,32,35,49))],
 'close':[('line',(16,16,48,48)),('line',(48,16,16,48))],
 'check':[('line',(11,33,26,48,54,17))],
 'lock':[('rect',(16,28,48,54)),('line',(22,28,22,17,27,10,37,10,42,17,42,28))],
 'night':[('line',(42,9,25,12,13,25,13,40,25,53,43,52,53,42,37,42,26,32,26,19,42,9))],
 'home':[('line',(7,29,32,8,57,29)),('line',(15,24,15,55,49,55,49,24)),('rect',(27,38,37,55))]
}
for name,parts in icons.items():
    im=Image.new('RGBA',(64,64));d=ImageDraw.Draw(im);svg=[]
    for kind,coords in parts:
        if kind=='line':
            pts=list(zip(coords[::2],coords[1::2]));d.line(pts,fill='white',width=3)
            svg.append('<polyline points="'+' '.join(f'{x},{y}' for x,y in pts)+'"/>')
        elif kind=='rect':
            d.rectangle(coords,outline='white',width=3);x,y,x2,y2=coords;svg.append(f'<rect x="{x}" y="{y}" width="{x2-x}" height="{y2-y}"/>')
        else:
            d.ellipse(coords,outline='white',width=3);x,y,x2,y2=coords;svg.append(f'<ellipse cx="{(x+x2)/2}" cy="{(y+y2)/2}" rx="{(x2-x)/2}" ry="{(y2-y)/2}"/>')
    im.save(OUT/('icon_'+name+'.png'))
    (SOURCE/('icon_'+name+'.svg')).write_text('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64"><g fill="none" stroke="white" stroke-width="3">'+''.join(svg)+'</g></svg>')
print(f'Created {len(icons)} icons and 7 sliced surfaces with editable SVG sources in {OUT}')

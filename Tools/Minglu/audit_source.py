import sys,zipfile,re
from html.parser import HTMLParser
from pathlib import Path
sys.stdout.reconfigure(encoding='utf-8')
class Text(HTMLParser):
    def __init__(self):super().__init__();self.skip=0;self.parts=[]
    def handle_starttag(self,t,a):
        if t in ('script','style'):self.skip+=1
        if t in ('h1','h2','h3','h4','p','tr','li','div','br'):self.parts.append('\n')
    def handle_endtag(self,t):
        if t in ('script','style'):self.skip-=1
        if t in ('h1','h2','h3','h4','p','tr','li','div'):self.parts.append('\n')
        if t in ('td','th'):self.parts.append(' | ')
    def handle_data(self,d):
        if not self.skip:self.parts.append(d)
out=Path('outputs/source-audit');out.mkdir(exist_ok=True)
with zipfile.ZipFile('C:/Users/11597/Downloads/明路地下_系统策划案_完整版_v2.zip') as z:
    for name in z.namelist():
        raw=z.read(name).decode('utf-8');p=Text();p.feed(raw)
        text='\n'.join(x.strip() for x in ''.join(p.parts).splitlines() if x.strip())
        (out/(Path(name).stem+'.txt')).write_text(text,encoding='utf-8')
        print('\n'+name)
        for h in re.findall(r'<h[1-4][^>]*>(.*?)</h[1-4]>',raw,re.S):
            q=Text();q.feed(h);print(' '.join(''.join(q.parts).split()))

"""Deterministic 1080p art direction studies. Pillow + NumPy, no network/engine."""
from pathlib import Path
from functools import lru_cache
import json, math, hashlib, textwrap
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = Path(__file__).resolve().parent
W, H = 1920, 1080
CASTLE = ('Every midnight the chapel bell rings with no hand on the rope, and every midnight one of us is written out of the book.\n\n'
          'I hid the silver blade where he cannot bear to look: in the reliquary box below, under the marks his own claws left. Light the ash and you will see it.')
PHOTO = "Four of them on the porch, summer. The youngest is holding the music box. Someone has scratched out the father's face."
OBJECTIVE = 'Find the Blood Ledger: steward · portrait · orrery — rooms left: chapel, dining hall'
WARNING = 'BREAK LINE OF SIGHT · hide [E] · lantern off [F] · stay quiet'
DOCS = ["Gate keeper's letter", 'Ledger: the steward', 'Ledger: the portrait', 'Ledger: the orrery', 'Altar inscription']
SCREENS = ['document_castle', 'document_bellwether', 'journal', 'hud', 'title', 'settings', 'ending_closed', 'ending_lost']
CONFIGS = {
 '01-casefile': dict(name="Investigator's casefile", short='CASEFILE', kind=1,
   bg='#181c1a', paper='#e6dfc9', ink='#252a26', accent='#813b2e', muted='#515548', dark='#111713', light='#eee9d8',
   display='courierprime/CourierPrime-Bold.ttf', body='courierprime/CourierPrime-Regular.ttf', mono='courierprime/CourierPrime-Regular.ttf'),
 '02-occult-folio': dict(name='The occult folio', short='OCCULT FOLIO', kind=2,
   bg='#151310', paper='#ded5bb', ink='#28231c', accent='#722b24', muted='#5c503e', dark='#151310', light='#e9dfc7',
   display='imfellenglish/IMFeENrm28P.ttf', body='imfellenglish/IMFeENrm28P.ttf', mono='imfellenglish/IMFeENrm28P.ttf'),
 '03-last-light': dict(name='Last light', short='LAST LIGHT', kind=3,
   bg='#0b1012', paper='#0b1012', ink='#eeeae0', accent='#e58b7b', muted='#a5b0b2', dark='#0b1012', light='#eeeae0',
   display='barlowcondensed/BarlowCondensed-SemiBold.ttf', body='barlowcondensed/BarlowCondensed-Regular.ttf', mono='barlowcondensed/BarlowCondensed-Regular.ttf'),
 '04-dead-channel': dict(name='Dead channel', short='DEAD CHANNEL', kind=4,
   bg='#071610', paper='#10261b', ink='#d3f8ad', accent='#efc17c', muted='#a3c493', dark='#071610', light='#d3f8ad',
   display='sharetechmono/ShareTechMono-Regular.ttf', body='courierprime/CourierPrime-Regular.ttf', mono='sharetechmono/ShareTechMono-Regular.ttf'),
 '05-two-moons': dict(name='Under two moons', short='TWO MOONS', kind=5,
   bg='#121116', paper='#121116', ink='#ece1c3', accent='#dfbb73', muted='#c4b797', dark='#121116', light='#ece1c3',
   display='unifrakturmaguntia/UnifrakturMaguntia-Book.ttf', body='imfellenglish/IMFeENrm28P.ttf', mono='imfellenglish/IMFeENrm28P.ttf'),
}

def rgb(c): return tuple(bytes.fromhex(c.lstrip('#')))
def lum(c):
    v = [x / 255 for x in rgb(c)]
    v = [x/12.92 if x <= .04045 else ((x+.055)/1.055)**2.4 for x in v]
    return sum(x*y for x,y in zip(v,[.2126,.7152,.0722]))
def contrast(a,b):
    x,y = sorted((lum(a),lum(b)))
    return (y+.05)/(x+.05)

@lru_cache(maxsize=32)
def scene(seed=4, house=False):
    """A procedural screenshot-like plate, not a claimed frame from the game."""
    rng=np.random.default_rng(seed)
    y,x=np.mgrid[:H,:W]
    glow=np.exp(-((x-1220)**2/280000+(y-440)**2/170000))
    a=np.zeros((H,W,3),dtype=float)
    for i,c in enumerate([12,17,20]): a[:,:,i]=c+glow*[19,23,27][i]+rng.normal(0,.9,(H,W))
    im=Image.fromarray(np.uint8(np.clip(a,0,255)))
    d=ImageDraw.Draw(im)
    if house:
        d.polygon([(700,590),(700,355),(1220,160),(1750,355),(1750,680)],fill='#151c20')
        for xx in [790,1010,1330,1550]:
            d.rectangle((xx,380,xx+100,540),fill='#2e3937')
            d.line((xx+50,380,xx+50,540),fill='#131a1b',width=5)
        d.rectangle((1140,430,1260,690),fill='#090e11')
        d.rectangle((660,605,1790,633),fill='#28312f')
        for xx in range(700,1790,100): d.rectangle((xx,610,xx+9,740),fill='#28312f')
    else:
        for xx,h in [(720,300),(960,170),(1270,260),(1590,110)]:
            d.rectangle((xx,h+90,xx+140,850),fill='#10171c')
            d.polygon([(xx-14,h+90),(xx+70,h-70),(xx+154,h+90)],fill='#10171c')
            for yy in range(h+150,690,130):
                d.rectangle((xx+56,yy,xx+77,yy+55),fill='#4c5147')
        d.polygon([(650,780),(650,600),(860,480),(1660,480),(1840,620),(1840,860)],fill='#0d1418')
        d.ellipse((1440,125,1530,215),fill='#76827a')
        d.ellipse((1460,112,1550,202),fill='#1d292e')
        d.ellipse((1610,230,1650,270),fill='#68766e')
    for xx in [50,360,1790]:
        d.polygon([(xx-60,H),(xx+22,0),(xx+56,0),(xx+60,H)],fill='#080d10')
        d.line([(xx,450),(xx+140,245),(xx+170,120)],fill='#080d10',width=16)
    # foreground stair and approach lines
    for yy in [870,940,1040]: d.line((500,yy,W,yy),fill='#202726',width=2)
    d.line((860,830,480,1080),fill='#232c2b',width=2)
    return im

@lru_cache(maxsize=1)
def photo():
    """4:3 drawn archival placeholder, deliberately not a real family photo."""
    rng=np.random.default_rng(107)
    a=np.zeros((540,720,3),dtype=np.uint8)
    noise=rng.integers(0,10,(540,720))
    for i,v in enumerate([112,111,98]): a[:,:,i]=v+noise
    im=Image.fromarray(a); d=ImageDraw.Draw(im)
    d.polygon([(0,145),(360,20),(720,145)],fill='#363b35')
    d.rectangle((0,145,720,425),fill='#8e9180')
    for yy in range(145,426,18): d.line((0,yy,720,yy),fill='#777e70',width=2)
    for xx in [40,580]:
        d.rectangle((xx,175,xx+100,305),fill='#39493f'); d.line((xx+50,175,xx+50,305),fill='#b8b69f',width=5)
    d.rectangle((280,155,430,405),fill='#434e41')
    for xx in [15,190,515,695]: d.rectangle((xx,135,xx+17,450),fill='#c4c1a6')
    for i,(cx,cy,hh) in enumerate([(245,245,183),(350,250,178),(440,294,130),(520,325,103)]):
        d.ellipse((cx-25,cy-35,cx+25,cy+16),fill='#353d32')
        d.polygon([(cx-28,cy+12),(cx+26,cy+12),(cx+39,cy+hh),(cx-40,cy+hh)],fill=['#555d4b','#3c493d','#6d715c','#4a5847'][i])
        if i==0:
            for n in range(8): d.line((cx-31,cy-34+n*6,cx+30,cy+15-n*5),fill='#c7c5b2',width=3)
        if i==3: d.rectangle((cx-28,cy+50,cx+27,cy+77),fill='#b1ad91',outline='#313c32',width=3)
    for yy in [453,483,517]: d.line((0,yy,720,yy),fill='#585e50',width=7)
    return im

class Study:
    def __init__(self,slug):
        self.slug=slug; self.c=CONFIGS[slug]; self.k=self.c['kind']; self.root=ROOT/slug
        self.root.mkdir(exist_ok=True); (self.root/'textures').mkdir(exist_ok=True)
        self.audit=[]; self.screen=''; self.im=None; self.d=None
    def f(self,role,size): return ImageFont.truetype(str(self.root/'fonts'/self.c[role]),size)
    def rect(self,box,fill,outline=None,width=1): self.d.rectangle(box,fill=fill,outline=outline,width=width)
    def line(self,pts,fill=None,width=2): self.d.line(pts,fill=fill or self.c['accent'],width=width)
    def text(self,txt,x,y,size=36,role='body',color=None,back=None,align='left',tracking=0,semantic=True):
        color=color or self.c['ink']; back=back or self.c['paper']; f=self.f(role,size)
        tw=(sum(self.d.textlength(ch,font=f) for ch in txt) if tracking else self.d.textlength(txt,font=f))+max(0,len(txt)-1)*tracking
        if align=='center': x-=tw/2
        if align=='right': x-=tw
        box=self.d.textbbox((x,y),txt,font=f,anchor='lt')
        box=(int(x),int(y),math.ceil(x+tw),int(box[3]))
        assert box[0]>=0 and box[1]>=0 and box[2]<=W and box[3]<=H,(self.slug,self.screen,txt,box)
        # Quiet backing is intentional: ornament, grain and scanlines never cross type.
        self.rect((box[0]-3,box[1]-3,box[2]+3,box[3]+3),back)
        if tracking:
            xx=x; baseline=y-f.getbbox(txt,anchor='ls')[1]
            for ch in txt:
                self.d.text((xx,baseline),ch,font=f,fill=color,anchor='ls'); xx+=self.d.textlength(ch,font=f)+tracking
        else: self.d.text((x,y),txt,font=f,fill=color,anchor='lt')
        self.audit.append(dict(screen=self.screen,text=txt,box=box,size=size,role=role,foreground=color,backing=back,
                               contrast=round(contrast(color,back),3),semantic=semantic))
        return box[3]
    def block(self,txt,x,y,width,size=40,role='body',color=None,back=None,leading=1.32):
        f=self.f(role,size); y0=y
        for para in txt.split('\n\n'):
            line=''
            for word in para.split():
                candidate=(line+' '+word).strip()
                if line and (self.d.textlength(candidate,font=f)>width or len(candidate)>70):
                    self.text(line,x,y,size,role,color,back); y+=round(size*leading); line=word
                else: line=candidate
            if line: self.text(line,x,y,size,role,color,back); y+=round(size*leading)
            y+=round(size*.55)
        return y
    def seal(self,cx,cy,r,color=None):
        col=color or self.c['accent']
        for rr in [r,r-8,r-16]: self.d.ellipse((cx-rr,cy-rr,cx+rr,cy+rr),outline=col,width=2)
        self.line([(cx,cy-r*.68),(cx+r*.5,cy+r*.48),(cx-r*.5,cy+r*.48),(cx,cy-r*.68)],col,3)
        self.line((cx-r*.65,cy,cx+r*.65,cy),col,2)
        self.d.ellipse((cx-8,cy-8,cx+8,cy+8),fill=col)
    def wax(self,cx,cy,r=40):
        rng=np.random.default_rng(191)
        pts=[]
        for angle in np.linspace(0,2*math.pi,70):
            rr=r+float(rng.uniform(-3,3)); pts.append((cx+rr*math.cos(angle),cy+rr*math.sin(angle)))
        self.d.polygon([(x+4,y+6) for x,y in pts],fill='#8b7d63')
        self.d.polygon(pts,fill='#722b24')
        self.d.arc((cx-r+5,cy-r+5,cx+r-5,cy+r-5),185,325,fill='#b6694c',width=3)
        self.seal(cx,cy,r*.65,'#c58b68')
    def woodcut(self,box,color=None):
        """Reliquary / chapel silhouette in ruled intaglio strokes."""
        x,y,x2,y2=box; col=color or self.c['accent']; cx=(x+x2)/2; w=x2-x; h=y2-y
        self.line([(x,y2),(x,y+h*.28),(cx,y),(x2,y+h*.28),(x2,y2)],col,4)
        self.line([(x+15,y2),(x+15,y+h*.3),(cx,y+25),(x2-15,y+h*.3),(x2-15,y2)],col,2)
        for n in range(12):
            yy=y+h*.45+n*10; self.line((x+25,yy,x2-25,yy),col,1)
        self.d.ellipse((cx-26,y+h*.2,cx+26,y+h*.2+52),outline=col,width=3)
        for xx in np.linspace(x+30,x2-30,8): self.line((xx,y+h*.64,xx,y2),col,1)
        self.seal(cx,y+h*.7,w*.23,col)
    def arch(self,box,color=None,width=2):
        x,y,x2,y2=box; cx=(x+x2)/2; col=color or self.c['accent']; q=(x2-x)/2
        self.line([(x,y2),(x,y+q),(x+q*.2,y+q*.45),(cx,y),(x2-q*.2,y+q*.45),(x2,y+q),(x2,y2)],col,width)
    def moons(self,cx,cy,r=35):
        self.d.ellipse((cx-r-35,cy-r,cx+r-35,cy+r),fill=self.c['accent'])
        self.d.ellipse((cx-r-22,cy-r-10,cx+r-22,cy+r-10),fill=self.c['bg'])
        self.d.ellipse((cx+48-r*.65,cy-r*.65,cx+48+r*.65,cy+r*.65),outline=self.c['accent'],width=3)
    def waveform(self,x,y,width,color=None):
        rng=np.random.default_rng(32); pts=[]
        for i in range(width):
            peak=sum(a*math.exp(-((i/width-c)/s)**2) for c,s,a in [(.2,.005,-38),(.215,.006,64),(.235,.011,-20),(.58,.005,-34),(.595,.005,62),(.615,.01,-20)])
            pts.append((x+i,y+peak+float(rng.uniform(-1.2,1.2))))
        self.line(pts,color or self.c['accent'],2)
    def base(self,screen):
        self.screen=screen; self.im=Image.new('RGB',(W,H),self.c['bg']); self.d=ImageDraw.Draw(self.im)
        # Sparse material grain outside protected reading fields.
        rng=np.random.default_rng(42+self.k)
        for _ in range(2200):
            x=int(rng.integers(W)); y=int(rng.integers(H)); col=tuple(min(255,v+int(rng.integers(0,6))) for v in rgb(self.c['bg']))
            self.d.point((x,y),fill=col)
        if self.k==1:
            self.rect((92,90,1828,1010),'#393f34'); self.rect((115,108,1805,990),'#b0a27d')
            self.rect((1340,69,1740,119),'#b0a27d')
            self.im.paste(Image.open(self.root/'textures/surface.png').resize((1620,800)),(150,150)); self.d=ImageDraw.Draw(self.im)
            self.rect((150,150,1770,205),'#d1c6a6')
            for xx in [155,158,1762]: self.line((xx,211,xx,943),'#c6bca3',1)
            self.line((150,950,1770,950),'#767461',3)
            for yy in [280,540,800]:
                self.d.ellipse((174,yy,195,yy+21),fill=self.c['bg']); self.d.arc((125,yy-5,197,yy+30),170,370,fill='#90988b',width=7)
            self.line((225,210,225,905),'#9b6c57',1)
        elif self.k==2:
            self.rect((202,62,1718,1017),'#080907')
            self.im.paste(Image.open(self.root/'textures/surface.png').resize((1480,915)),(220,80)); self.d=ImageDraw.Draw(self.im)
            for xx in [205,210,215]: self.line((xx,85,xx,995),'#594735',1)
            for off in [0,7,16]: self.rect((239+off,100+off,1681-off,974-off),None,self.c['ink'],2 if off else 7)
            for xx in [276,1644]:
                for yy in range(145,930,64): self.d.polygon([(xx,yy-13),(xx+7,yy),(xx,yy+13),(xx-7,yy)],fill=self.c['accent'])
        elif self.k==3:
            self.line((100,88,100,970),self.c['accent'],3)
            self.arch((1420,165,1760,960),'#263235',2)
            self.arch((1433,190,1747,960),'#1c272b',1)
        elif self.k==4:
            self.rect((45,37,1875,1043),'#0b2117', '#55774d',2)
            for yy in range(43,1040,6): self.line((48,yy,1871,yy),'#10281a',1)
            self.rect((66,64,1854,1016),self.c['paper'])
            for yy in range(70,1010,7): self.line((67,yy,1853,yy),'#142b1c',1)
            for xx,yy,dx,dy in [(85,83,1,1),(1835,83,-1,1),(85,997,1,-1),(1835,997,-1,-1)]:
                self.line([(xx,yy+24*dy),(xx,yy),(xx+30*dx,yy)],self.c['accent'],3)
        elif self.k==5:
            self.rect((73,45,1847,1035),None,'#55472d',1)
            for xx in [120,1800]:
                self.line((xx,96,xx,984),'#8a7048',2)
                for yy in [160,900]: self.seal(xx,yy,24)
            self.arch((158,62,1762,1000),'#55472d',2)
            # Paired narrow lancets echo the castle's stone mullions, outside reading fields.
            for xx in [168,1680]:
                self.arch((xx,250,xx+70,910),'#665333',1)
                self.arch((xx+14,285,xx+56,910),'#463b2a',1)
            self.moons(960,96,28)
    def save(self):
        self.im.save(self.root/f'{self.screen}.png')
    def esc(self,ink=None,back=None,y=965):
        if self.k==1: x=1740; y=906
        elif self.k==2: x=1618; y=924
        elif self.k==3: x=1790
        elif self.k==4: x=1790
        else: x=1728; y=960
        self.text('ESC',x,y,30,'mono',ink or self.c['ink'],back or self.c['paper'],align='right')
    def document(self,isphoto=False):
        self.base('document_bellwether' if isphoto else 'document_castle')
        c=self.c; k=self.k
        title='Family photograph' if isphoto else 'The steward'
        if k==1:
            self.text('HOLLOW CURRENT / FIELD EVIDENCE',270,166,26,'mono',c['ink'],'#d1c6a6')
            self.text('02' if isphoto else '07',1650,245,80,'display',c['accent'],align='right')
            self.text('BELLWETHER / IMAGE 01' if isphoto else 'BLOOD LEDGER',275,250,31,'display',c['accent'])
            self.text(title,275,310,70,'display')
            self.line((275,407,1675,407),c['ink'],2)
            if isphoto:
                self.rect((274,443,1036,871),'#f5f0e2')
                self.im.paste(photo().resize((520,390)),(390,458)); self.d=ImageDraw.Draw(self.im)
                self.block(PHOTO,1090,455,560,38)
                self.text('TRANSCRIPT / 1994',1090,780,27,'mono',c['accent'])
            else:
                self.block(CASTLE,380,453,1190,40,leading=1.31)
                self.text('RECOVERED',282,853,31,'display',c['accent'])
                self.line((275,841,530,841),c['accent'],3)
                self.text('CHAPEL ARCHIVE / FOLIO 03',740,862,27,'mono',c['muted'])
        elif k==2:
            self.text('Memoria mortuorum',960,145,30,'body',c['accent'],align='center')
            self.text(title,960,203,95,'display',align='center')
            self.line((350,330,1570,330),c['ink'],3)
            if isphoto:
                self.rect((335,393,1055,823),'#24231d')
                self.im.paste(photo().resize((520,390)),(430,410)); self.d=ImageDraw.Draw(self.im)
                self.block(PHOTO,1110,399,450,42)
            else:
                self.woodcut((337,401,543,802),c['ink'])
                self.text('BLOOD LEDGER',650,380,30,'body',c['accent'],tracking=3)
                self.block(CASTLE,650,452,890,43,leading=1.31)
            self.wax(960,899,39)
        elif k==3:
            self.text('BELLWETHER / RECOVERED IMAGE' if isphoto else 'BLOOD LEDGER',155,112,32,'mono',c['accent'],tracking=4)
            self.text(title,148,190,142,'display')
            self.line((156,386,1790,386),'#526063',1)
            if isphoto:
                self.im.paste(photo().resize((720,540)),(158,425)); self.d=ImageDraw.Draw(self.im)
                self.block(PHOTO,1010,460,600,46)
                self.text('SUMMER / 1994',1010,828,32,'mono',c['muted'],tracking=2)
            else:
                self.block(CASTLE,158,460,1010,46,leading=1.36)
                self.text('03',1640,797,130,'display',c['muted'])
                self.text('THE CHAPEL',1465,937,29,'mono',c['muted'],tracking=3)
        elif k==4:
            self.text('PLAY >  CH-04',120,100,35,'mono')
            self.text('00:13:07:24',1795,100,35,'mono',align='right')
            self.line((120,165,1795,165),c['muted'],2)
            self.text('EVIDENCE',120,224,64,'display')
            self.text('CAMERA / 02' if isphoto else 'BLOOD LEDGER',120,316,30,'mono',c['accent'])
            self.text('FOCUS LOCKED',120,390,28,'mono',c['muted'])
            self.waveform(120,570,355,c['ink'])
            for i in range(12): self.rect((120+i*28,690-(i%5)*15,137+i*28,725),c['ink'] if i<7 else '#395e38')
            self.text('TAPE REMAIN  26:53',120,815,26,'mono',c['muted'])
            self.rect((545,207,1796,913),'#d8ddbc'); ink='#193022'; paper='#d8ddbc'
            self.text(title,595,245,69,'display',ink,paper)
            self.line((595,348,1746,348),ink,2)
            if isphoto:
                self.im.paste(photo().resize((580,435)),(590,390)); self.d=ImageDraw.Draw(self.im)
                self.block(PHOTO,1220,400,510,36,color=ink,back=paper)
            else:
                self.block(CASTLE,625,414,1090,39,color=ink,back=paper,leading=1.32)
            self.text('SP  /  ARCHIVE RECORD',120,960,30,'mono',c['accent'])
        else:
            self.text('Bellwether, a remembrance' if isphoto else 'BLOOD LEDGER',960,160,32,'body',c['accent'],align='center',tracking=2)
            self.text(title,960,232,112,'display',align='center')
            self.line((356,380,1564,380),c['accent'],2)
            if isphoto:
                self.arch((284,414,1054,938),c['accent'],2)
                self.im.paste(photo().resize((640,480)),(350,450)); self.d=ImageDraw.Draw(self.im)
                self.block(PHOTO,1120,471,490,44)
            else:
                self.woodcut((275,445,485,842),c['accent'])
                self.text('III',380,886,46,'body',c['accent'],align='center')
                self.block(CASTLE,610,450,990,45,leading=1.31)
                self.line((610,870,1600,870),'#746140',1)
        self.esc(); self.save()

    def heading(self,kicker,title):
        k=self.k
        x=270 if k==1 else 335 if k==2 else 158 if k==3 else 120 if k==4 else 290
        y=240 if k==1 else 145 if k==2 else 115 if k==3 else 110 if k==4 else 170
        self.text(kicker,x,y,30,'mono',self.c['accent'],tracking=2 if k in [3,5] else 0)
        self.text(title,x,y+63,76 if k in [1,4] else 100,'display')
        return x,y+195
    def journal(self):
        self.base('journal'); c=self.c; k=self.k
        x,y=self.heading('HAUNTED CASTLE / INVESTIGATION','Case journal' if k!=2 else 'Book of witnesses')
        if k==1: y=417
        elif k==2: y=373
        elif k==3: y=355
        elif k==4: y=340
        else: y=395
        right=1090 if k!=2 else 1060
        self.text('RECOVERED DOCUMENTS',x,y,29,'mono',c['muted'])
        self.text('CONCLUSIONS',right,y,29,'mono',c['muted'])
        for i,name in enumerate(DOCS):
            yy=y+70+i*67
            if i==1:
                self.rect((x-12,yy-12,right-65,yy+45),c['ink'])
                self.text(name,x+24,yy,36,'body',c['paper'],c['ink'])
            else: self.text(name,x+24,yy,36)
            nx=x-40 if x>200 else x+670
            on_selected=i==1 and x<=200
            self.text(str(i+1).zfill(2),nx,yy+4,25,'mono',c['paper'] if on_selected else c['muted'],c['ink'] if on_selected else c['paper'])
        values=[('Anchor','The chapel bell'),('Pattern',"The bargain's toll"),('Closure','Let the last stroke fade')]
        for i,(label,value) in enumerate(values):
            yy=y+62+i*114
            self.text(label,right,yy,30,'mono',c['accent'])
            self.text(value,right+28,yy+43,34)
            edge=1585 if k==2 else 1780 if k==4 else 1670
            self.text('‹',right,yy+42,35); self.text('›',edge,yy+42,35)
            self.line((right,yy+87,edge,yy+87),c['muted'],1)
        wy=876 if k==1 else 845
        self.waveform(x,wy,right-x-100)
        self.text('DREAD',x,wy-62,27,'mono',c['muted'])
        self.text('Close the case',right,wy-10,41,'display',c['accent'])
        self.line((right,wy+45,1600,wy+45),c['accent'],2)
        self.esc(); self.save()

    def hud(self):
        self.base('hud'); c=self.c; k=self.k
        self.im=scene().copy(); self.d=ImageDraw.Draw(self.im)
        bg=c['dark']; fg=c['light']; ac=c['accent'] if k>2 else '#e2c08c'
        if k==1:
            bg=c['paper']; fg=c['ink']; ac=c['accent']; box=(72,64,1375,210)
        elif k==2: box=(350,64,1570,210)
        elif k==3: box=(96,64,1200,210)
        elif k==4: box=(90,115,1490,260)
        else: box=(360,64,1560,210)
        self.rect(box,bg)
        if k==1: self.rect((72,64,86,210),ac)
        elif k==2:
            self.line((370,82,1550,82),ac,2)
            self.seal(260,138,40,ac)
        elif k==3: self.line((96,64,96,210),ac,3)
        elif k==4:
            self.text('REC  /  CH-04',90,65,30,'mono',c['ink'],c['dark'])
            self.text('00:13:07:24',1820,65,30,'mono',c['ink'],c['dark'],align='right')
            for xx,yy,dx,dy in [(60,40,1,1),(1860,40,-1,1),(60,1040,1,-1),(1860,1040,-1,-1)]:
                self.line([(xx,yy+30*dy),(xx,yy),(xx+38*dx,yy)],ac,2)
        else:
            self.line((380,84,1540,84),ac,1)
            self.moons(220,135,28)
        self.block(OBJECTIVE,box[0]+40,box[1]+41,box[2]-box[0]-80,35,'mono',fg,bg,1.25)
        # Prompt and notice use opaque quiet strips for reliable scene contrast.
        self.rect((650,625,1290,685),c['dark'])
        self.text('Open Great door [E]',970,641,36,'mono',c['light'],c['dark'],align='center')
        self.rect((715,716,1225,780),c['dark'])
        self.text('The door is held.',970,734,36,'body',c['light'],c['dark'],align='center')
        self.rect((72,882,570,1018),c['dark'])
        self.text('STAMINA',96,904,28,'mono',c['light'],c['dark'])
        self.line((96,957,325,957),'#45504c',5); self.line((96,957,265,957),c['light'],5)
        self.text('LANTERN  62%',350,903,27,'mono',c['light'],c['dark'])
        self.rect((350,948,528,960),'#45504c'); self.rect((350,948,460,960),ac)
        self.rect((640,882,1840,1018),c['dark'])
        self.line((662,903,662,993),ac,4)
        self.block(WARNING,692,909,1110,32,'mono',c['light'],c['dark'],1.25)
        self.save()

    def map_card(self,box,name,house=False,selected=False):
        x,y,x2,y2=box; c=self.c
        plate=scene(8 if house else 4,house).crop((550,100,1850,900)).resize((x2-x,y2-y))
        self.im.paste(plate,(x,y)); self.d=ImageDraw.Draw(self.im)
        self.rect((x,y2-66,x2,y2),c['dark'])
        self.text(name,x+24,y2-50,38,'body',c['light'],c['dark'])
        if selected:
            self.rect(box,None,c['accent'],3)
            self.rect((x+17,y+17,x+147,y+56),c['dark'])
            self.text('SELECTED',x+27,y+25,25,'mono',c['light'],c['dark'])
    def title(self):
        self.base('title'); c=self.c; k=self.k
        x=265 if k==1 else 330 if k==2 else 155 if k==3 else 120 if k==4 else 285
        y=245 if k==1 else 220 if k==2 else 210 if k==3 else 215 if k==4 else 245
        self.text('HOLLOW',x,y,118 if k==1 else 145 if k==3 else 118,'display')
        self.text('CURRENT',x,y+133,118 if k==1 else 145 if k==3 else 118,'display')
        self.text('A CASE OF THE UNEXPLAINED',x,y-72,26,'mono',c['accent'],tracking=1)
        for i,item in enumerate(['Continue','New case','Settings','Quit']):
            yy=y+325+i*68
            self.text(item,x+40,yy,40,'body',c['ink'] if i else c['accent'])
            if i==0: self.line((x,yy+13,x+22,yy+13),c['accent'],3)
        rx=1000 if k in [1,2,5] else 1020
        end=1620 if k==2 else 1700 if k in [1,5] else 1795
        self.map_card((rx,250,end,505),'14 Bellwether Lane',True)
        self.map_card((rx,545,end,800),'Haunted Castle',False,True)
        self.text('CHOOSE A CASE',rx,845,28,'mono',c['accent'],tracking=1)
        if k==2: self.wax(960,905,34)
        elif k==5: self.moons(1420,900,25)
        self.save()
    def settings(self):
        self.base('settings'); c=self.c; k=self.k
        x,y=self.heading('PAUSED / HOLLOW CURRENT','Settings')
        if k==1: y=450
        else: y=max(y,390)
        self.text('Continue',x,y+14,43,'body',c['accent'])
        for i,item in enumerate(['New case','Settings','Quit']): self.text(item,x,y+100+i*75,38)
        rx=850 if k in [1,2] else 780
        rw=800 if k!=2 else 720
        labels=['Look sensitivity','Volume','Windowed','Invert Y']
        for i,label in enumerate(labels):
            yy=y+i*110
            self.text(label,rx,yy,37)
            if i<2:
                self.text('55%' if i==0 else '80%',rx+rw,yy,32,'mono',c['accent'],align='right')
                self.line((rx,yy+67,rx+rw,yy+67),c['muted'],2)
                pos=rx+rw*(.55 if i==0 else .8)
                self.line((rx,yy+67,pos,yy+67),c['accent'],4)
                self.rect((pos-7,yy+57,pos+7,yy+77),c['ink'])
            else:
                self.text('ON' if i==2 else 'OFF',rx+rw,yy,32,'mono',c['accent'],align='right')
                self.line((rx,yy+68,rx+rw,yy+68),c['muted'],1)
        self.esc(); self.save()
    def ending(self,lost=False):
        self.base('ending_lost' if lost else 'ending_closed'); c=self.c; k=self.k
        title='SIGNAL LOST' if lost else 'CASE CLOSED'
        epitaph='The house has learned another name.' if lost else 'At dawn, the bell kept its silence.'
        if k==1:
            self.text('FIELD OFFICE / FINAL DISPOSITION',960,245,29,'mono',c['muted'],align='center')
            self.rect((430,402,1490,562),None,c['accent'],6)
            self.text(title,960,444,82,'display',c['accent'],align='center')
            self.text(epitaph,960,644,36,'body',align='center')
            self.waveform(660,794,600)
        elif k==2:
            self.woodcut((846,170,1074,410),c['ink'])
            self.text(title,960,496,91,'display',c['accent'],align='center')
            self.text(epitaph,960,658,44,'body',align='center')
            self.wax(960,828,43)
        elif k==3:
            self.text('HOLLOW CURRENT',156,118,31,'mono',c['muted'],tracking=4)
            self.text(title,150,420,160,'display')
            self.line((158,626,310,626),c['accent'],4)
            self.text(epitaph,158,692,46)
            self.text('00' if lost else '01',1630,817,140,'display',c['muted'])
        elif k==4:
            self.text('TAPE END / 00:39:57:12',120,110,34,'mono',c['accent'])
            self.text(title,960,390,130,'display',align='center')
            if lost: self.line((350,604,1570,604),c['ink'],2)
            else: self.waveform(350,604,1220,c['ink'])
            self.text(epitaph,960,717,38,'body',align='center')
            self.text('NO CARRIER' if lost else 'RECORD SAVED',960,870,30,'mono',c['accent'],align='center')
        else:
            self.arch((530,211,1390,870),c['accent'],2)
            self.moons(960,300,54)
            # This protected field keeps the arch clear of large type.
            self.rect((270,470,1650,650),c['bg'])
            self.text(title,960,487,123,'display',align='center')
            self.text(epitaph,960,711,43,'body',align='center')
            self.seal(960,867,37)
        self.esc(); self.save()
    def textures(self):
        # Each sheet is an independent engine-ready procedural source texture.
        tex=self.root/'textures'; c=self.c
        p=Image.new('RGB',(512,512),c['paper']); d=ImageDraw.Draw(p); rng=np.random.default_rng(700+self.k)
        for _ in range(9000):
            x,y=rng.integers(0,512,2)
            if x<48 or x>464 or y<48 or y>464:
                base=rgb(c['paper']); dd=int(rng.integers(-10,7))
                d.point((int(x),int(y)),fill=tuple(max(0,min(255,v+dd)) for v in base))
        p.save(tex/'surface.png')
        oldim,oldd=self.im,self.d
        self.im=Image.new('RGBA',(512,512),(0,0,0,0)); self.d=ImageDraw.Draw(self.im)
        self.rect((15,15,496,496),None,c['accent'],3)
        if self.k==1:
            for yy in [100,256,412]: self.d.ellipse((26,yy-9,44,yy+9),outline=c['ink'],width=3)
        elif self.k==2:
            self.rect((25,25,486,486),None,c['ink'],7); self.seal(80,80,30)
        elif self.k==3: self.line((15,15,15,496),c['accent'],6)
        elif self.k==4:
            for yy in range(32,490,12): self.line((20,yy,34,yy),c['accent'],1)
        else:
            for xx in [60,452]:
                for yy in [60,452]: self.seal(xx,yy,24)
        self.im.save(tex/'frame.png')
        self.im=Image.new('RGBA',(512,512),(0,0,0,0)); self.d=ImageDraw.Draw(self.im)
        self.seal(85,85,50); self.woodcut((200,26,300,160)); self.arch((360,30,460,160)); self.waveform(25,280,450)
        self.im.save(tex/'icon_atlas.png'); self.im,self.d=oldim,oldd
        photo().save(tex/'photo_placeholder.png'); scene().save(tex/'castle_plate.png'); scene(8,True).save(tex/'bellwether_plate.png')
    def render(self):
        self.textures(); self.document(); self.document(True); self.journal(); self.hud(); self.title(); self.settings(); self.ending(); self.ending(True)
        (self.root/'layout_audit.json').write_text(json.dumps(self.audit,indent=2)+'\n')
        print(f'{self.slug}: 8 PNGs rendered; {len(self.audit)} measured text runs.')

def render(slug): Study(slug).render()
if __name__=='__main__':
    import sys
    for slug in sys.argv[1:] or CONFIGS: render(slug)

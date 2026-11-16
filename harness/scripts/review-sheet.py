import sys,glob,os
from PIL import Image, ImageDraw
src,dst,kind=sys.argv[1],sys.argv[2],sys.argv[3]
fs=sorted(glob.glob(os.path.join(src,f'*_{kind}.png')))
os.makedirs(dst,exist_ok=True)
per=12
for k in range(0,len(fs),per):
    chunk=fs[k:k+per]
    W,H=480,270; cols=3; rows=(len(chunk)+cols-1)//cols
    im=Image.new('RGB',(W*cols,H*rows),'black')
    for i,f in enumerate(chunk):
        t=Image.open(f); im.paste(t,((i%cols)*W,(i//cols)*H))
        d=ImageDraw.Draw(im); d.rectangle([(i%cols)*W,(i//cols)*H,(i%cols)*W+70,(i//cols)*H+22],fill='black'); d.text(((i%cols)*W+4,(i//cols)*H+4),os.path.basename(f).split('_')[0]+'b',fill='yellow')
    im.save(os.path.join(dst,f'{kind}_{k//per:02d}.jpg'),quality=85)
print(len(fs))

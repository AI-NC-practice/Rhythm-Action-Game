# 작업 R03: R02 배경 그림 6장(Assets/Art/Backdrops/bg_*.png)을 이어 붙여 둘레를 한 번 도는 파노라마 한 장을 만든다.
# 이음매는 겹쳐서 가로로 서서히 섞는다(마지막 장 → 첫 장도 섞어 둘레가 이어지게). 같은 그림이 되풀이되지 않는다.
# 사용: python harness/scripts/map/make-backdrop.py   (Pillow 필요)
import os
import sys
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
SRC = os.path.join(ROOT, "Assets", "Art", "Backdrops")
ORDER = ["bg_roofs_a", "bg_hill_a", "bg_mountain_a", "bg_roofs_b", "bg_hill_b", "bg_mountain_b"]   # 비슷한 그림끼리 붙지 않게
H = 768
OVERLAP = 0.18   # 장마다 양옆으로 겹쳐 섞는 비율

imgs = []
for n in ORDER:
    im = Image.open(os.path.join(SRC, n + ".png")).convert("RGB")
    w = round(im.width * H / im.height)
    imgs.append(im.resize((w, H), Image.LANCZOS))

ov = round(imgs[0].width * OVERLAP)
step = [im.width - ov for im in imgs]
total = sum(step)
pano = Image.new("RGB", (total, H))
x = 0
for i, im in enumerate(imgs):
    # 앞부분 ov 픽셀은 앞 장의 끝과 섞는다 (첫 장은 마지막 장의 끝과)
    prev = imgs[i - 1]
    tail = prev.crop((prev.width - ov, 0, prev.width, H))
    head = im.crop((0, 0, ov, H))
    mask = Image.linear_gradient("L").rotate(90, expand=True).resize((ov, H))   # 왼쪽 0 → 오른쪽 255
    mask = mask.transpose(Image.FLIP_LEFT_RIGHT)
    blend = Image.composite(tail, head, mask)   # 왼쪽은 앞 장, 오른쪽으로 갈수록 이번 장
    pano.paste(blend, (x, 0))
    pano.paste(im.crop((ov, 0, im.width, H)), (x + ov, 0))
    x += step[i]
out = os.path.join(SRC, "panorama.png")
pano.save(out, optimize=True)
print(out, pano.size, os.path.getsize(out) // 1024, "KB")

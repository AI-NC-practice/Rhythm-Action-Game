"""asset_specs.json의 에셋 한 개에서 한 필드만 바꾼다 (파일의 손으로 쓴 한 줄 배열·객체 모양을 그대로 둔다).

사용: python -I harness/scripts/varco/spec-set.py <에셋 id> <필드> <JSON 값>
예:   python -I harness/scripts/varco/spec-set.py gate nodes '{ "text": "a", "image": "b", "mesh": null }'
      python -I harness/scripts/varco/spec-set.py gate status '"generated"'
필드는 그 에셋 블록 안에서 처음 나오는 "<필드>": 줄을 바꾼다 (값은 한 줄이어야 함).
"""
import io
import json
import re
import sys

path = "harness/asset_specs.json"
aid, field, value = sys.argv[1], sys.argv[2], sys.argv[3]
json.loads(value)   # 값이 JSON인지 확인
text = io.open(path, encoding="utf-8").read()
m = re.search(r'"id":\s*"%s"' % re.escape(aid), text)
if not m:
    sys.exit("에셋 없음: " + aid)
end = text.find('"id":', m.end())
end = len(text) if end < 0 else end
block = text[m.start():end]
pat = re.compile(r'("%s":\s*)([^\n]*?)(,?)(\n)' % re.escape(field))
fm = pat.search(block)
if not fm:
    sys.exit(f"필드 없음: {aid}.{field}")
block = block[:fm.start()] + fm.group(1) + value + fm.group(3) + fm.group(4) + block[fm.end():]
text = text[:m.start()] + block + text[end:]
json.loads(text)   # 전체가 여전히 JSON인지 확인
io.open(path, "w", encoding="utf-8", newline="\n").write(text)
print(f"{aid}.{field} = {value}")

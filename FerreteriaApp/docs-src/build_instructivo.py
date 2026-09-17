import re, sys, os
S = os.path.dirname(os.path.abspath(__file__))
P = os.path.dirname(S)  # carpeta del proyecto (docs-src está adentro)
tpl = open(os.path.join(S, "instructivo.template.md"), encoding="utf-8").read()

missing = []
def repl_file(m):
    path, lang = m.group(1), m.group(2)
    full = os.path.join(P, path)
    if not os.path.exists(full):
        missing.append(path); return f"**(archivo no encontrado: {path})**"
    code = open(full, encoding="utf-8").read().rstrip("\n")
    return f"`{path}`\n\n```{lang}\n{code}\n```"

out = re.sub(r"\{\{FILE:([^:}]+):([a-z]+)\}\}", repl_file, tpl)

readme = open(os.path.join(P, "README.md"), encoding="utf-8").read()
# Bajar un nivel los títulos del README para que queden dentro del anexo
lines = []
in_code = False
for ln in readme.splitlines():
    if ln.startswith("```"):
        in_code = not in_code
    if not in_code and re.match(r"^#{1,5} ", ln):
        ln = "#" + ln
    lines.append(ln)
out = out.replace("{{README}}", "\n".join(lines))

if missing:
    print("FALTAN:", missing); sys.exit(1)

open(os.path.join(P, "INSTRUCTIVO.md"), "w", encoding="utf-8").write(out)
print("INSTRUCTIVO.md:", out.count("\n"), "líneas,", len(out)//1024, "KB")

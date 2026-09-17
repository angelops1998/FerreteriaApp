import markdown, os, subprocess, sys, html
S = os.path.dirname(os.path.abspath(__file__))
P = os.path.dirname(S)  # carpeta del proyecto (docs-src está adentro)
md = open(os.path.join(P, "INSTRUCTIVO.md"), encoding="utf-8").read()
body = markdown.markdown(md, extensions=["fenced_code", "tables", "sane_lists"])
css = """
body { font-family: Calibri, Carlito, Arial, sans-serif; font-size: 11pt; }
h1 { font-size: 20pt; color: #1F3864; page-break-before: always; }
h1.first { page-break-before: auto; }
h2 { font-size: 15pt; color: #2E5395; }
h3 { font-size: 12.5pt; color: #2E5395; }
h4 { font-size: 11.5pt; color: #404040; }
p, li { line-height: 1.25; }
code { font-family: Consolas, 'Liberation Mono', monospace; font-size: 9pt; color: #7A1F1F; }
pre { font-family: Consolas, 'Liberation Mono', monospace; font-size: 8.5pt; background-color: #F2F2F2; padding: 6pt; border: 1px solid #D9D9D9; white-space: pre-wrap; }
pre code { color: #000000; font-size: 8.5pt; }
table { border-collapse: collapse; width: 100%; font-size: 9.5pt; }
th, td { border: 1px solid #A6A6A6; padding: 3pt 5pt; vertical-align: top; }
th { background-color: #D9E2F3; }
blockquote { border-left: 3px solid #FFC000; background-color: #FFF8E1; padding: 4pt 8pt; margin-left: 0; }
hr { border: 0; border-top: 1px solid #BFBFBF; }
"""
# El primer h1 (título) no debe empezar en página nueva
body = body.replace("<h1>", '<h1 class="first">', 1)
body = body.replace("<table>", '<table border="1" cellspacing="0" cellpadding="4">')
doc = f'<!DOCTYPE html><html><head><meta charset="utf-8"><title>Instructivo FerreteriaApp</title><style>{css}</style></head><body>{body}</body></html>'
html_path = os.path.join(S, "INSTRUCTIVO.html")
open(html_path, "w", encoding="utf-8").write(doc)
print("HTML:", len(doc)//1024, "KB")

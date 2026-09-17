# Cómo regenerar INSTRUCTIVO.md / .docx / .pdf

`instructivo.template.md` es el texto del instructivo con marcadores `{{FILE:ruta:lenguaje}}` que se
reemplazan por el contenido real de cada archivo del proyecto (así el código del documento siempre
coincide con el código fuente), y `{{README}}` que inserta el README.md.

1. Editar `instructivo.template.md` (o el código del proyecto).
2. Generar el Markdown final (solo necesita Python 3):

```bash
python3 docs-src/build_instructivo.py
```

3. (Opcional) Generar el HTML intermedio y convertirlo a Word y PDF (necesita `pip install markdown`
   y LibreOffice):

```bash
python3 docs-src/md2docx.py
libreoffice --headless --infilter="HTML (StarWriter)" --convert-to "docx:MS Word 2007 XML" docs-src/INSTRUCTIVO.html
libreoffice --headless --convert-to pdf INSTRUCTIVO.docx
```

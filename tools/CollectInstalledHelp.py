import pathlib,hashlib,json,re,html.parser
r=pathlib.Path(r'C:\Projects\CodeAssist');installed=pathlib.Path(r'C:\Users\ehdwn\AppData\Local\Microsoft\VisualStudio\17.0_ca39936f\Extensions\zgy5sun1.aim')
class Text(html.parser.HTMLParser):
 def __init__(self):super().__init__();self.parts=[];self.links=[];self.hidden=0
 def handle_starttag(self,tag,attrs):
  if tag in ['script','style']:self.hidden+=1
  if tag=='a':
   url=dict(attrs).get('href')
   if url:self.links.append(url)
 def handle_endtag(self,tag):
  if tag in ['script','style']:self.hidden=max(0,self.hidden-1)
 def handle_data(self,text):
  if not self.hidden:self.parts.append(text)
records=[];snapshots=[]
for group in ['OptionsHelp','OptKeyBindsHelp']:
 for p in sorted((installed/group).rglob('*.html')):
  raw=p.read_bytes();parser=Text();parser.feed(raw.decode('utf-8-sig',errors='replace'));text=' '.join(' '.join(parser.parts).split())
  if not text:continue
  rel=str(p.relative_to(installed)).replace('\\','/');links=sorted(set(x for x in parser.links if 'wholetomato' in x.lower()))
  records.append({'file':rel,'sha256':hashlib.sha256(raw).hexdigest(),'category':p.parent.name,'resourceStem':p.stem,'textCharacters':len(text),'officialLinks':links,'mentionsDefault':bool(re.search(r'\bdefault\b',text,re.I)),'scope':'Shipped 2630 help resource; filename is not proof of registry binding or enabled availability'})
  snapshots.append({'file':rel,'text':text})
(r/'tools/obj/rea/installed-help-text.json').write_text(json.dumps(snapshots,ensure_ascii=False),encoding='utf-8')
(r/'docs/validation/va-installed-help-catalog.json').write_text(json.dumps({'installedRoot':str(installed),'build':2630,'count':len(records),'records':records,'limitations':['Help resource existence does not prove command availability.','Help text references may point to newer online documentation.','Factory defaults require source or reset-profile observations.']},ensure_ascii=False,indent=2),encoding='utf-8')
print('Installed help resources:',len(records),'Options:',sum(x['file'].startswith('OptionsHelp/') for x in records),'Key commands:',sum(x['file'].startswith('OptKeyBindsHelp/') for x in records))
for name in ['AutoReparse','AutoReparseOnBattery','MaxThreads','ParseConcurrency','ParserThreads','MaxParses','Reparse','Queue','SortSelectedLines']:
 for x in snapshots:
  if pathlib.Path(x['file']).stem.lower()==name.lower():print(x['file'],x['text'][:300])

import sys,json
sys.path.insert(0,'tools/ParityRunner');from background_steps import setup,save;from background_client import module
text='class RelBase103\r\n{\r\npublic:\r\n    virtual int Act() { return 1; }\r\n};\r\nclass RelDerived103 : public RelBase103\r\n{\r\npublic:\r\n    int Act() override { return 2; }\r\n};\r\n'
for role in ['VA','CA']:setup(role,'related-symbols',text,text.index('RelDerived103')+3)
v,p=module('CA','index-reinitialize',windowTitle='',_timeout=60);save('CA.related-symbols.index-preparation.json',v);print(v['status'],v.get('result'),str(v.get('error'))[:300],flush=True)
v,p=module('CA','index-state',windowTitle='',query='RelDerived103');save('CA.related-symbols.index-ready.json',v);print(v.get('result'),flush=True)

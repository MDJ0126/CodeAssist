"""Supervisor tests use explicit synthetic workers, not VA parity evidence."""
import json,sys,tempfile,time,unittest
from pathlib import Path
import bounded
SCRIPT=r'''
import json,sys,time
from pathlib import Path
q=json.loads(Path(sys.argv[1]).read_text());m=q['case'].get('testMode','quick');role=q['role'];started=time.time()
if m=='hang':time.sleep(8)
if m=='shared':time.sleep(3)
if m=='crash':raise SystemExit(3)
if m=='missing':raise SystemExit(0)
b={'text':'','caret':0,'selectionStart':0,'selectionLength':0,'selectedText':''};a={'text':'()','caret':1,'selectionStart':1,'selectionLength':0,'selectedText':''}
Path(q['result']).write_text(json.dumps(dict(token='wrong' if m=='stale' else q['token'],role=role,pid=q['pid'],status='complete',before=b,after=a,workerStarted=started,workerFinished=time.time())),encoding='utf-8')
'''
class BoundedTests(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory();self.root=Path(self.tmp.name);self.script=self.root/'worker.py';self.script.write_text(SCRIPT);self.commands={r:[sys.executable,str(self.script)] for r in ['VA','CA']};self.case=dict(id='sample',text='',start=0,length=0,compareFields=list(bounded.FIELDS));self.targets={'VA':101,'CA':202}
 def tearDown(self):self.tmp.cleanup()
 def run_sample(self,mode='quick',**kw):return bounded.run_case(dict(self.case,testMode=mode),self.targets,self.commands,self.root/mode,**kw)
 def test_quick_pair(self):
  r=self.run_sample();self.assertEqual(r['status'],'representative-match');self.assertLess(r['elapsedMs'],5000)
 def test_modal_and_input_serialization(self):
  r=self.run_sample();self.assertGreaterEqual(r['roles']['CA']['workerStarted'],r['roles']['VA']['workerFinished'])
 def test_hung_worker_capped_and_next_item_continues(self):
  q=set();r=self.run_sample('hang',quarantined=q);self.assertEqual(r['status'],'timeout');self.assertLess(r['elapsedMs'],5000);self.assertIn(101,q)
  r=bounded.run_case(self.case,{'VA':303,'CA':404},self.commands,self.root/'next',q);self.assertEqual(r['status'],'representative-match')
 def test_shared_pair_budget_not_five_each(self):
  r=self.run_sample('shared');self.assertEqual(r['status'],'timeout');self.assertLess(r['elapsedMs'],5000)
 def test_crash_not_pass(self):self.assertEqual(self.run_sample('crash')['status'],'unverified')
 def test_missing_result_not_pass(self):self.assertEqual(self.run_sample('missing')['status'],'unverified')
 def test_stale_token_rejected(self):self.assertEqual(self.run_sample('stale')['status'],'unverified')
 def test_quarantined_host_not_retried(self):
  r=self.run_sample(quarantined={101});self.assertEqual(r['roles']['VA']['status'],'host-quarantined');self.assertFalse((self.root/'quick/VA').exists())
 def test_no_adapter_not_pass(self):
  self.case.pop('compareFields');self.assertEqual(self.run_sample()['status'],'observed-only')
 def test_cannot_raise_work_limit(self):
  with self.assertRaises(ValueError):self.run_sample(work_seconds=5.1)
 def test_noop_rejected(self):
  r=self.run_sample();v=r['roles']['VA'];a=r['roles']['CA'];v['after']=v['before'];a['after']=a['before'];self.assertEqual(bounded.compare(self.case,{'VA':v,'CA':a})[0],'unverified')
 def test_incomplete_snapshot_rejected(self):
  r=self.run_sample();r['roles']['CA']['after'].pop('caret');self.assertEqual(bounded.compare(self.case,r['roles'])[0],'unverified')
if __name__=='__main__':unittest.main(verbosity=2)

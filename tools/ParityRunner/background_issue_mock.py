import sys,json,threading,time
from http.server import ThreadingHTTPServer,BaseHTTPRequestHandler
sys.path.insert(0,'tools/ParityRunner');from background_client import module;from background_steps import OUT,save,setup
requests=[]
class Handler(BaseHTTPRequestHandler):
 def log_message(self,*args):pass
 def do_GET(self):
  requests.append(dict(path=self.path,userAgent=self.headers.get('User-Agent')))
  if self.path.startswith('/fog/api.asp?'):
   data=b'<response><cases count="1"><case ixBug="103"><sTitle>Parity FogBugz issue 103</sTitle><sStatus>Active</sStatus><sPersonAssignedTo>Fixture103</sPersonAssignedTo><dtOpened>2026-10-19T00:00:00Z</dtOpened><dtResolved></dtResolved><sLatestTextSummary>Owned FogBugz fixture comment</sLatestTextSummary></case></cases></response>';kind='application/xml'
  elif '/repos/Parity103/Repo103/issues/103' in self.path:
   data=json.dumps(dict(id=103,number=103,title='Parity GitHub issue 103',body='Owned GitHub fixture comment',state='open',assignee={'login':'fixture103','name':'Fixture103'},assignees=[],comments=0,created_at='2026-10-19T00:00:00Z',updated_at='2026-10-19T00:00:00Z',closed_at=None)).encode();kind='application/json'
  else:self.send_error(404);return
  self.send_response(200);self.send_header('Content-Type',kind);self.send_header('Content-Length',str(len(data)));self.send_header('X-RateLimit-Limit','5000');self.send_header('X-RateLimit-Remaining','4999');self.end_headers();self.wfile.write(data)
server=ThreadingHTTPServer(('127.0.0.1',0),Handler);thread=threading.Thread(target=server.serve_forever,daemon=True);thread.start();base='http://127.0.0.1:'+str(server.server_port)+'/'
try:
 v,_=module('VA','native-issue-mock',windowTitle='',baseUrl=base,_timeout=30);save('VA.issue-mock.actual.json',v);print('VA',v,flush=True);nativeRequests=len(requests);setup('CA','issue-links','// GH#103 CASE103\r\nint main() { return 0; }\r\n',7);v,_=module('CA','ca-issue-info',windowTitle='',baseUrl=base);save('CA.issue-mock.actual.json',v);print('CA',v,flush=True);save('issue-mock.requests.json',dict(baseUrl=base,requests=requests,requestsBeforeCa=nativeRequests,requestsAfterCa=len(requests),realServiceCredentialsUsed=False))
finally:server.shutdown();server.server_close();thread.join(timeout=2)

"""Attach scoped dialog clients to a running opt-in VS fixture suite.

No product edits are submitted. A case-ID handshake prevents a late client from
touching a subsequent dialog. Sensor evidence stays separate from core results.
"""
import base64
import concurrent.futures
import json
import os
from pathlib import Path
import subprocess
import sys
import time
import ctypes
from ctypes import wintypes


def read_shared(path):
    kernel=ctypes.WinDLL('kernel32',use_last_error=True)
    kernel.CreateFileW.argtypes=[wintypes.LPCWSTR,wintypes.DWORD,wintypes.DWORD,ctypes.c_void_p,wintypes.DWORD,wintypes.DWORD,wintypes.HANDLE]
    kernel.CreateFileW.restype=wintypes.HANDLE
    kernel.CloseHandle.argtypes=[wintypes.HANDLE]
    kernel.GetFileSizeEx.argtypes=[wintypes.HANDLE,ctypes.POINTER(ctypes.c_longlong)]
    kernel.ReadFile.argtypes=[wintypes.HANDLE,ctypes.c_void_p,wintypes.DWORD,ctypes.POINTER(wintypes.DWORD),ctypes.c_void_p]
    handle=kernel.CreateFileW(str(path),0x80000000,7,None,3,0x80,None)
    if handle==ctypes.c_void_p(-1).value:raise OSError(ctypes.get_last_error())
    try:
        size=ctypes.c_longlong()
        if not kernel.GetFileSizeEx(handle,ctypes.byref(size)) or size.value>32*1024*1024:raise OSError('invalid report size')
        buffer=ctypes.create_string_buffer(size.value)
        read=wintypes.DWORD()
        if not kernel.ReadFile(handle,buffer,size.value,ctypes.byref(read),None):raise OSError(ctypes.get_last_error())
        return buffer.raw[:read.value].decode('utf-8-sig')
    finally:kernel.CloseHandle(handle)


def monitor(root,role):
    plan=json.loads((root/(role+'.plan.json')).read_text(encoding='utf-8-sig'))
    known={c['id']:c for c in plan['cases']}
    attempted=set()
    deadline=time.monotonic()+600
    while time.monotonic()<deadline:
        try:state=json.loads(read_shared(root/(role+'.results.json')))
        except (OSError,ValueError):time.sleep(.15);continue
        if state.get('completed'):
            print(role,'completed',len(state['cases']),'fatal',state.get('fatal'),flush=True)
            return
        case=known.get(state.get('executing'))
        if not case or case['action']!='dialog-probe' or case['id'] in attempted:
            time.sleep(.1);continue
        attempted.add(case['id'])
        output=root/(role+'-'+case['id']+'.dialog.json')
        env=dict(os.environ,CODEASSIST_PARITY_ROOT=str(root))
        host=Path(__file__).parent/'bin/Release/CodeAssist.DialogProbeHost.exe'
        if not host.exists():host=Path(__file__).with_name('CodeAssist.DialogProbeHost.exe')
        args=[str(host),str(state['processId']),
              base64.b64encode(case.get('page','').encode()).decode(), '6000',str(case.get('filePicker',False)),str(output)]
        try:
            result=subprocess.run(args,env=env,capture_output=True,text=True,timeout=12,creationflags=subprocess.CREATE_NO_WINDOW)
            data=json.loads(output.read_text()) if output.exists() else {'status':'sensor-failed','error':result.stderr[:500]}
            data['evidenceRunId']=plan['runId'];data['evidenceRole']=role;data['evidenceCaseId']=case['id'];data['evidenceProcessId']=state['processId']
            output.write_text(json.dumps(data,ensure_ascii=False),encoding='utf-8')
            print(role,case['id'],data.get('status'),flush=True)
        except Exception as error:print(role,case['id'],str(error),flush=True)
    print(role,'coordinator-timeout',flush=True)


if __name__=='__main__':
    root=Path(sys.argv[1]).resolve()
    if not (root/'.codeassist-parity-owned').exists():raise SystemExit('Not an owned run')
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        list(pool.map(lambda role:monitor(root,role),['VA','CA']))

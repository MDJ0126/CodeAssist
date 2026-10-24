#include <windows.h>
#include <winnt.h>
extern "C" IMAGE_DOS_HEADER __ImageBase;
static COLORREF seen;static int recorded;static COLORREF inks[128];static int xs[128],ys[128],lengths[128];static void Record(HDC dc,int x,int y,int length){seen=GetTextColor(dc);if(recorded<128){inks[recorded]=seen;xs[recorded]=x;ys[recorded]=y;lengths[recorded]=length;recorded++;}}
static BOOL WINAPI SpyText(HDC dc,int x,int y,LPCWSTR s,int n){Record(dc,x,y,n);return TRUE;}
static BOOL WINAPI SpyExt(HDC dc,int x,int y,UINT f,const RECT* r,LPCWSTR s,UINT n,const INT* d){Record(dc,x,y,n);return TRUE;}
static void Spies(){
 BYTE* base=(BYTE*)&__ImageBase;auto nt=(IMAGE_NT_HEADERS*)(base+((IMAGE_DOS_HEADER*)base)->e_lfanew);
 auto desc=(IMAGE_IMPORT_DESCRIPTOR*)(base+nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT].VirtualAddress);
 for(;desc->Name;desc++){if(!desc->OriginalFirstThunk)continue;auto names=(IMAGE_THUNK_DATA*)(base+desc->OriginalFirstThunk);auto slots=(IMAGE_THUNK_DATA*)(base+desc->FirstThunk);
 for(;names->u1.AddressOfData;names++,slots++){if(IMAGE_SNAP_BY_ORDINAL(names->u1.Ordinal))continue;auto name=(IMAGE_IMPORT_BY_NAME*)(base+names->u1.AddressOfData);void* hook=nullptr;
 if(!strcmp((char*)name->Name,"TextOutW"))hook=(void*)&SpyText;else if(!strcmp((char*)name->Name,"ExtTextOutW"))hook=(void*)&SpyExt;
 if(hook){DWORD old;VirtualProtect(&slots->u1.Function,sizeof(void*),PAGE_READWRITE,&old);slots->u1.Function=(ULONG_PTR)hook;DWORD tmp;VirtualProtect(&slots->u1.Function,sizeof(void*),old,&tmp);}}}
}
extern "C" __declspec(dllexport) HWND CreateProbe(){static bool ready=false;if(!ready){WNDCLASSW wc={};wc.lpfnWndProc=DefWindowProcW;wc.hInstance=(HINSTANCE)&__ImageBase;wc.lpszClassName=L"CodeAssistLiteTreeTest";RegisterClassW(&wc);Spies();ready=true;}return CreateWindowW(L"CodeAssistLiteTreeTest",L"",WS_POPUP,0,0,160,60,nullptr,nullptr,(HINSTANCE)&__ImageBase,nullptr);}
extern "C" __declspec(dllexport) COLORREF DrawProbe(HWND window,int mode){HDC dc=GetDC(window);SetTextColor(dc,0x998877);if(mode==0)TextOutW(dc,0,0,L"Fixture",7);else if(mode==1)ExtTextOutW(dc,0,0,0,nullptr,L"Fixture",7,nullptr);else {WORD glyphs[7];GetGlyphIndicesW(dc,L"Fixture",7,glyphs,1);ExtTextOutW(dc,0,0,ETO_GLYPH_INDEX,nullptr,(LPCWSTR)glyphs,7,nullptr);}auto restored=GetTextColor(dc);ReleaseDC(window,dc);return restored;}
extern "C" __declspec(dllexport) COLORREF DrawNamed(HWND window,LPCWSTR text){HDC dc=GetDC(window);SetTextColor(dc,0x998877);TextOutW(dc,0,0,text,lstrlenW(text));auto restored=GetTextColor(dc);ReleaseDC(window,dc);return restored;}
extern "C" __declspec(dllexport) COLORREF GetSeen(){return seen;}
extern "C" __declspec(dllexport) void CloseProbe(HWND window){DestroyWindow(window);}
extern "C" __declspec(dllexport) void ResetRecords(){recorded=0;}
extern "C" __declspec(dllexport) int RecordCount(){return recorded;}
extern "C" __declspec(dllexport) COLORREF RecordInk(int i){return inks[i];}
extern "C" __declspec(dllexport) int RecordX(int i){return xs[i];}
extern "C" __declspec(dllexport) int RecordY(int i){return ys[i];}
extern "C" __declspec(dllexport) void DrawSignature(HWND window,int mode){LPCWSTR text=L"Tick(float DeltaTime) const";int n=lstrlenW(text);HDC dc=GetDC(window);SetTextColor(dc,0x998877);int dx[128];for(int i=0;i<128;i++)dx[i]=(mode==3&&i%2)?1:7;WORD glyphs[64];GetGlyphIndicesW(dc,text,n,glyphs,1);if(mode==0)TextOutW(dc,10,20,text,n);else ExtTextOutW(dc,10,20,(mode>=2?ETO_GLYPH_INDEX:0)|(mode==3?ETO_PDY:0),nullptr,mode>=2?(LPCWSTR)glyphs:text,n,dx);ReleaseDC(window,dc);}
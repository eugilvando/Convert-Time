#define UNICODE
#include <windows.h>
#include <shlobj.h>
#include <wrl/client.h>
#include <stdio.h>
using Microsoft::WRL::ComPtr;
int wmain(int argc,wchar_t** argv) {
    if(argc!=3) return 2;
    CoInitializeEx(nullptr,COINIT_APARTMENTTHREADED);
    HMODULE dll=LoadLibraryW(argv[1]);if(!dll)return 3;
    auto factory=(HRESULT(STDAPICALLTYPE*)(REFCLSID,REFIID,void**))GetProcAddress(dll,"DllGetClassObject");if(!factory)return 4;
    PIDLIST_ABSOLUTE pidl=nullptr;
    if(FAILED(SHParseDisplayName(argv[2],nullptr,&pidl,0,nullptr)))return 5;
    ComPtr<IShellItemArray> items;
    PCIDLIST_ABSOLUTE selection[]={pidl};auto hr=SHCreateShellItemArrayFromIDLists(1,selection,&items);CoTaskMemFree(pidl);if(FAILED(hr))return 6;
    const wchar_t* ids[]={L"{5095c8cb-bba1-4781-b915-e62e33224dd6}",L"{f7931b5d-aed9-4c77-920e-b35b64768a14}",L"{a11fbc1c-96eb-4643-acc9-6c4fc444dcea}"};
    for(int i=0;i<3;i++){
        CLSID clsid;CLSIDFromString(ids[i],&clsid);ComPtr<IClassFactory> cf;ComPtr<IExplorerCommand> cmd;
        if(FAILED(factory(clsid,IID_PPV_ARGS(&cf))) || FAILED(cf->CreateInstance(nullptr,IID_PPV_ARGS(&cmd))))return 7;
        EXPCMDSTATE state; if(FAILED(cmd->GetState(items.Get(),TRUE,&state)) || state!=ECS_ENABLED)return 8+i;
        PWSTR title=nullptr;if(FAILED(cmd->GetTitle(items.Get(),&title)) || !title)return 11;
        wprintf(L"%ls\n",title);CoTaskMemFree(title);
        if(i==2 && FAILED(cmd->Invoke(items.Get(),nullptr)))return 12;
    }
    return 0;
}
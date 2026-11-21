#define UNICODE
#define _UNICODE
#include <windows.h>
#include <shlobj.h>
#include <shlwapi.h>
#include <msxml6.h>
#include <wrl.h>
#include <wrl/module.h>
#include <string>
#include <vector>
#include <memory>
#include <limits>
using namespace Microsoft::WRL;

namespace {
struct CloseHandleDeleter { void operator()(HANDLE h) const { if (h && h != INVALID_HANDLE_VALUE) CloseHandle(h); } };
using Handle = std::unique_ptr<void, CloseHandleDeleter>;
struct FreeShellString { void operator()(wchar_t* p) const { CoTaskMemFree(p); } };
using ShellString = std::unique_ptr<wchar_t, FreeShellString>;
enum class Action { Open, Png, Webp };
const wchar_t* profile(Action action) { return action == Action::Png ? L"To Png" : L"To Webp"; }

std::wstring application() {
    wchar_t value[32768]; DWORD size = sizeof(value);
    if (RegGetValueW(HKEY_LOCAL_MACHINE, L"Software\\ConvertTime", L"Path",
        RRF_RT_REG_SZ | RRF_SUBKEY_WOW6464KEY, nullptr, value, &size) != ERROR_SUCCESS) return {};
    return value;
}
HRESULT paths(IShellItemArray* selection, std::vector<std::wstring>& result) {
    if (!selection) return E_INVALIDARG;
    DWORD count = 0; HRESULT hr = selection->GetCount(&count);
    if (FAILED(hr) || !count) return E_INVALIDARG;
    for (DWORD i = 0; i < count; ++i) {
        ComPtr<IShellItem> item; PWSTR value = nullptr;
        if (FAILED(hr = selection->GetItemAt(i,&item)) || FAILED(hr = item->GetDisplayName(SIGDN_FILESYSPATH,&value))) return hr;
        ShellString path(value); result.emplace_back(path.get());
    }
    return S_OK;
}
bool accepts(Action action, const std::vector<std::wstring>& files, const std::wstring& executable) {
    if (action == Action::Open) return true;
    PWSTR folder = nullptr;
    if (FAILED(SHGetKnownFolderPath(FOLDERID_LocalAppData,0,nullptr,&folder))) return false;
    ShellString owned(folder); std::wstring filename = std::wstring(folder) + L"\\ConvertTime\\Settings.user.xml";
    if (!PathFileExistsW(filename.c_str())) filename = executable.substr(0,executable.find_last_of(L'\\')+1) + L"Settings.default.xml";
    ComPtr<IXMLDOMDocument2> doc;
    if (FAILED(CoCreateInstance(__uuidof(DOMDocument60),nullptr,CLSCTX_INPROC_SERVER,IID_PPV_ARGS(&doc)))) return false;
    doc->put_async(VARIANT_FALSE); doc->put_validateOnParse(VARIANT_FALSE); doc->put_resolveExternals(VARIANT_FALSE);
    VARIANT source; VariantInit(&source); source.vt=VT_BSTR; source.bstrVal=SysAllocString(filename.c_str());
    VARIANT_BOOL loaded=VARIANT_FALSE; HRESULT hr=doc->load(source,&loaded); VariantClear(&source);
    if (FAILED(hr) || !loaded) return false;
    std::wstring query = std::wstring(L"/Settings/ConversionPreset[@Name='") + profile(action) + L"']/InputTypes";
    BSTR xpath=SysAllocString(query.c_str()); ComPtr<IXMLDOMNodeList> nodes; hr=doc->selectNodes(xpath,&nodes); SysFreeString(xpath);
    if (FAILED(hr) || !nodes) return false;
    std::vector<std::wstring> extensions; ComPtr<IXMLDOMNode> node;
    while (nodes->nextNode(&node) == S_OK && node) {
        BSTR text=nullptr;
        if (SUCCEEDED(node->get_text(&text)) && text) { extensions.emplace_back(text); SysFreeString(text); }
        node.Reset();
    }
    for (auto& file:files) {
        auto extension=PathFindExtensionW(file.c_str()); bool matched=false;
        if (*extension) for(auto& ext:extensions) if (!_wcsicmp(extension+1,ext.c_str())) {matched=true;break;}
        if (!matched) return false;
    }
    return true;
}

// A single inherited delete-on-close file carries arbitrarily long Explorer selections.
// No selected path or user-supplied command is interpreted by a shell.
HRESULT launch(const std::wstring& exe, Action action, const std::vector<std::wstring>& files) {
    wchar_t tempDir[MAX_PATH], filename[MAX_PATH];
    DWORD len=GetTempPathW(MAX_PATH,tempDir);
    if (!len || len>=MAX_PATH || !GetTempFileNameW(tempDir,L"ctm",0,filename)) return HRESULT_FROM_WIN32(GetLastError());
    SECURITY_ATTRIBUTES sa{sizeof(sa),nullptr,TRUE};
    Handle list(CreateFileW(filename,GENERIC_READ|GENERIC_WRITE|DELETE,0,&sa,OPEN_EXISTING,FILE_ATTRIBUTE_TEMPORARY|FILE_FLAG_DELETE_ON_CLOSE,nullptr));
    if(list.get()==INVALID_HANDLE_VALUE){ auto err=GetLastError();DeleteFileW(filename);return HRESULT_FROM_WIN32(err);}
    for(auto& f:files) {
        std::wstring line=f+L"\n"; DWORD written=0, bytes=static_cast<DWORD>(line.size()*sizeof(wchar_t));
        if(!WriteFile(list.get(),line.data(),bytes,&written,nullptr) || written!=bytes) return E_FAIL;
    }
    LARGE_INTEGER start{}; if(!SetFilePointerEx(list.get(),start,nullptr,FILE_BEGIN)) return HRESULT_FROM_WIN32(GetLastError());
    SIZE_T size=0;InitializeProcThreadAttributeList(nullptr,1,0,&size);
    std::vector<BYTE> storage(size); auto attributes=reinterpret_cast<PPROC_THREAD_ATTRIBUTE_LIST>(storage.data());
    if(!InitializeProcThreadAttributeList(attributes,1,0,&size)) return HRESULT_FROM_WIN32(GetLastError());
    std::unique_ptr<_PROC_THREAD_ATTRIBUTE_LIST,decltype(&DeleteProcThreadAttributeList)> cleanup(attributes,DeleteProcThreadAttributeList);
    HANDLE childHandle=list.get();
    if(!UpdateProcThreadAttribute(attributes,0,PROC_THREAD_ATTRIBUTE_HANDLE_LIST,&childHandle,sizeof(childHandle),nullptr,nullptr)) return HRESULT_FROM_WIN32(GetLastError());
    std::wstring args=L"\""+exe+L"\" --shell-handle "+std::to_wstring(reinterpret_cast<ULONG_PTR>(childHandle));
    if(action!=Action::Open) args+= std::wstring(L" --conversion-preset \"")+profile(action)+L"\"";
    STARTUPINFOEXW si{};si.StartupInfo.cb=sizeof(si);si.lpAttributeList=attributes;
    PROCESS_INFORMATION pi{};
    if(!CreateProcessW(exe.c_str(),args.data(),nullptr,nullptr,TRUE,EXTENDED_STARTUPINFO_PRESENT,nullptr,nullptr,&si.StartupInfo,&pi)) return HRESULT_FROM_WIN32(GetLastError());
    CloseHandle(pi.hThread);CloseHandle(pi.hProcess);return S_OK;
}
template<Action A> class Command : public RuntimeClass<RuntimeClassFlags<ClassicCom>,IExplorerCommand> {
public:
    IFACEMETHODIMP GetTitle(IShellItemArray*,PWSTR* out) override {return SHStrDupW(A==Action::Open?L"Abrir no Convert Time…":A==Action::Png?L"Converter para PNG":L"Converter para WebP",out);}
    IFACEMETHODIMP GetIcon(IShellItemArray*,PWSTR* out) override {try{return SHStrDupW((application()+L",0").c_str(),out);}catch(...){return E_OUTOFMEMORY;}}
    IFACEMETHODIMP GetToolTip(IShellItemArray*,PWSTR* out) override {*out=nullptr;return E_NOTIMPL;}
    IFACEMETHODIMP GetCanonicalName(GUID* id) override {*id=GUID_NULL;return E_NOTIMPL;}
    IFACEMETHODIMP GetState(IShellItemArray* items,BOOL slow,EXPCMDSTATE* state) override {
        *state=ECS_HIDDEN;if(!items)return S_OK;
        if(A==Action::Open){DWORD count=0;if(SUCCEEDED(items->GetCount(&count)) && count) *state=ECS_ENABLED;return S_OK;}
        if(!slow)return E_PENDING;
        try{std::vector<std::wstring> files;auto exe=application();if(!exe.empty() && SUCCEEDED(paths(items,files)) && accepts(A,files,exe)) *state=ECS_ENABLED;return S_OK;}catch(...){return E_OUTOFMEMORY;}
    }
    IFACEMETHODIMP Invoke(IShellItemArray* items,IBindCtx*) override {
        try{auto exe=application();if(exe.empty())return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);std::vector<std::wstring> files;auto hr=paths(items,files);if(FAILED(hr))return hr;if(!accepts(A,files,exe))return E_INVALIDARG;return launch(exe,A,files);}catch(...){return E_OUTOFMEMORY;}
    }
    IFACEMETHODIMP GetFlags(EXPCMDFLAGS* flags) override {*flags=ECF_DEFAULT;return S_OK;}
    IFACEMETHODIMP EnumSubCommands(IEnumExplorerCommand** items) override {*items=nullptr;return E_NOTIMPL;}
};
}
class __declspec(uuid("5095c8cb-bba1-4781-b915-e62e33224dd6")) OpenCommand final:public Command<Action::Open>{};
class __declspec(uuid("a11fbc1c-96eb-4643-acc9-6c4fc444dcea")) PngCommand final:public Command<Action::Png>{};
class __declspec(uuid("f7931b5d-aed9-4c77-920e-b35b64768a14")) WebpCommand final:public Command<Action::Webp>{};
CoCreatableClass(OpenCommand);
CoCreatableClass(PngCommand);
CoCreatableClass(WebpCommand);
STDAPI DllGetClassObject(REFCLSID clsid,REFIID iid,void** object){return Module<InProc>::GetModule().GetClassObject(clsid,iid,object);}
STDAPI DllCanUnloadNow(){return Module<InProc>::GetModule().GetObjectCount()==0?S_OK:S_FALSE;}
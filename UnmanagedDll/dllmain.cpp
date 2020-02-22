// dllmain.cpp : Defines the entry point for the DLL application.

#include "pch.h"
#include <wtypes.h>
#include <memory>
#include <tchar.h>
#include <cstdio>
#include "releaseHelper.h"
#include <iostream>
#include "resource.h"
#include <filesystem>
#using "D:\HOOK\InlineHook\Inject\bin\Debug\ManagedDll.dll"
using namespace std;
using namespace System;
using namespace Reflection;



HINSTANCE __stdcall GetInstanceFromAddress(PVOID pEip)
{
    _ASSERTE(pEip != NULL);
    MEMORY_BASIC_INFORMATION mem;
    if (VirtualQuery(pEip, &mem, sizeof(mem)))
    {
        _ASSERTE(mem.Type == MEM_IMAGE);
        _ASSERTE(mem.AllocationBase != NULL);
        return (HINSTANCE)mem.AllocationBase;
    }
    return NULL;
}


__declspec(naked)
HINSTANCE __stdcall GetCurrentInstance()
{
    __asm
    {
#ifdef _M_IX86
        mov eax, [esp]
        push eax
        jmp GetInstanceFromAddress
#else
# error This machine type is not supported.
#endif
    }
}

//ManagedDll.dll
bool extractResource(const HINSTANCE hInstance, WORD resourceID, const char* m_strResourceType, LPCTSTR szFilename)
{
    bool bSuccess = false;
    try
    {
        HRSRC hResource = FindResource(hInstance, MAKEINTRESOURCE(resourceID), m_strResourceType);
        HGLOBAL hFileResource = LoadResource(hInstance, hResource);
        LPVOID lpFile = LockResource(hFileResource);
        DWORD dwSize = SizeofResource(hInstance, hResource);
        HANDLE hFile = CreateFile(szFilename, GENERIC_READ | GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
        HANDLE hFileMap = CreateFileMapping(hFile, NULL, PAGE_READWRITE, 0, dwSize, NULL);
        LPVOID lpAddress = MapViewOfFile(hFileMap, FILE_MAP_WRITE, 0, 0, 0);
        CopyMemory(lpAddress, lpFile, dwSize);
        UnmapViewOfFile(lpAddress);
        CloseHandle(hFileMap);
        CloseHandle(hFile);
        bSuccess = true;
    }
    catch (...)
    {
        // Whatever
    }
    return bSuccess;
}


void MyTestFunction()
{
    ManagedDll::Class1^ ClassFunc = gcnew ManagedDll::Class1();
    ClassFunc->StartHook();
}

DWORD WINAPI InitHookThread(LPVOID dllMainThread)
{
    // 等待DllMain（LoadLibrary线程）结束
    WaitForSingleObject(dllMainThread, INFINITE);
    CloseHandle(dllMainThread);
    TCHAR dll_path[MAX_PATH] = { 0 };
    GetModuleFileName(GetCurrentInstance(), dll_path, MAX_PATH);
    string strPath = (string)dll_path;;
    int pos = strPath.find_last_of('\\', strPath.length());
    string Dll_Dir = strPath.substr(0, pos) + "\\ManagedDll.dll";

    BOOL bSuccess = extractResource(GetCurrentInstance(), IDR_DLLS2, "DLLS",Dll_Dir.c_str());
    if (bSuccess)
    {
        MyTestFunction();
    }  
   
    return 0;   
}


#pragma managed(push, off) // 保证native
BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        // 取当前线程句柄
        HANDLE curThread;
        if (!DuplicateHandle(GetCurrentProcess(), GetCurrentThread(), GetCurrentProcess(), &curThread, SYNCHRONIZE, FALSE, 0))
            return FALSE;
        // DllMain中不能运行托管代码，所以要在另一个线程初始化
        CloseHandle(CreateThread(NULL, 0, InitHookThread, curThread, 0, NULL));
        break;

    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}
#pragma managed(pop)


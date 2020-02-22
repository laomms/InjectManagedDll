// dllmain.cpp : Defines the entry point for the DLL application.

#include "pch.h"
#include <wtypes.h>
#include <memory>
#include <tchar.h>
#include <cstdio>
#using "D:\InlineHook-master\Inject\bin\Debug\ManagedDll.dll"
//using namespace ManagedDll::Class1;
using namespace std;
using namespace System;
using namespace Reflection;



#pragma pack(pop)    

DWORD WINAPI InitHookThread(LPVOID dllMainThread)
{
    // 等待DllMain（LoadLibrary线程）结束
    WaitForSingleObject(dllMainThread, INFINITE);
    CloseHandle(dllMainThread);

    ManagedDll::Class1^ ClassFunc = gcnew ManagedDll::Class1();
    ClassFunc->StartHook();
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


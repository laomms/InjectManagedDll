// dllmain.cpp : Defines the entry point for the DLL application.

#include "pch.h"
#include <wtypes.h>
#include <memory>
#include <tchar.h>
#include <cstdio>
#using "E:\HOOK\HookMessageBox\Inject\bin\Debug\ManagedDll.dll"
//using namespace ManagedDll::Class1;
using namespace std;
using namespace System;
using namespace Reflection;


class InlineHook
{
private:
#pragma pack(push)
#pragma pack(1)
#ifndef _WIN64
    class JmpCode
    {
    private:
        const BYTE m_code = 0xE9;
        uintptr_t m_address = 0;

    public:
        JmpCode() = default;

        JmpCode(uintptr_t srcAddr, uintptr_t dstAddr)
        {
            SetAddress(srcAddr, dstAddr);
        }

        void SetAddress(uintptr_t srcAddr, uintptr_t dstAddr)
        {
            m_address = dstAddr - srcAddr - sizeof(JmpCode);
        }
    };
#else
    struct JmpCode
    {
    private:
        const BYTE m_code[6] = { 0xFF, 0x25, 0x00, 0x00, 0x00, 0x00 };
        uintptr_t m_address = 0;

    public:
        JmpCode() = default;

        JmpCode(uintptr_t srcAddr, uintptr_t dstAddr)
        {
            SetAddress(srcAddr, dstAddr);
        }

        void SetAddress(uintptr_t srcAddr, uintptr_t dstAddr)
        {
            m_address = dstAddr;
        }
    };
#endif
#pragma pack(pop)


    void* const m_originalFunction = nullptr;
    void* const m_hookFunction = nullptr;
    bool m_isEnabled = false;
    BYTE m_oldCode[sizeof(JmpCode)];

public:
    InlineHook(void* originalFunction, void* hookFunction, bool enable = true) :
        m_originalFunction(originalFunction),
        m_hookFunction(hookFunction)
    {
        memcpy(m_oldCode, m_originalFunction, sizeof(m_oldCode));

        if (enable)
            Enable();
    }

    ~InlineHook()
    {
        Disable();
    }


    void Enable()
    {
        if (m_isEnabled)
            return;

        JmpCode code((uintptr_t)m_originalFunction, (uintptr_t)m_hookFunction);
        DWORD oldProtect, oldProtect2;
        VirtualProtect(m_originalFunction, sizeof(JmpCode), PAGE_EXECUTE_READWRITE, &oldProtect);
        memcpy(m_originalFunction, &code, sizeof(JmpCode));
        VirtualProtect(m_originalFunction, sizeof(JmpCode), oldProtect, &oldProtect2);

        m_isEnabled = true;
    }

    void Disable()
    {
        if (!m_isEnabled)
            return;

        DWORD oldProtect, oldProtect2;
        VirtualProtect(m_originalFunction, sizeof(JmpCode), PAGE_EXECUTE_READWRITE, &oldProtect);
        memcpy(m_originalFunction, m_oldCode, sizeof(JmpCode));
        VirtualProtect(m_originalFunction, sizeof(JmpCode), oldProtect, &oldProtect2);

        m_isEnabled = false;
    }
};


DWORD WINAPI InitHookThread(LPVOID dllMainThread)
{
    // �ȴ�DllMain��LoadLibrary�̣߳�����
    WaitForSingleObject(dllMainThread, INFINITE);
    CloseHandle(dllMainThread);

    ManagedDll::Class1^ ClassFunc = gcnew ManagedDll::Class1();
    ClassFunc->StartHook();
    return 0;   
}


#pragma managed(push, off) // ��֤native
BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved
)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        // ȡ��ǰ�߳̾��
        HANDLE curThread;
        if (!DuplicateHandle(GetCurrentProcess(), GetCurrentThread(), GetCurrentProcess(), &curThread, SYNCHRONIZE, FALSE, 0))
            return FALSE;
        // DllMain�в��������йܴ��룬����Ҫ����һ���̳߳�ʼ��
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


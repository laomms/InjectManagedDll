Imports System.Runtime.InteropServices

Public Class Form1
    Public Enum ThreadAccess As Integer
        TERMINATE = (&H1)
        SUSPEND_RESUME = (&H2)
        GET_CONTEXT = (&H8)
        SET_CONTEXT = (&H10)
        SET_INFORMATION = (&H20)
        QUERY_INFORMATION = (&H40)
        SET_THREAD_TOKEN = (&H80)
        IMPERSONATE = (&H100)
        DIRECT_IMPERSONATION = (&H200)
    End Enum
    Public Declare Function VirtualAllocEx Lib "kernel32" (ByVal hProcess As Integer, ByVal lpAddress As Integer, ByVal dwSize As Integer, ByVal flAllocationType As Integer, ByVal flProtect As Integer) As Integer
    Public Const MEM_COMMIT = 4096, PAGE_EXECUTE_READWRITE = &H40
    Public Declare Function WriteProcessMemory Lib "kernel32" (ByVal hProcess As Integer, ByVal lpBaseAddress As Integer, ByVal lpBuffer As Byte(), ByVal nSize As Integer, ByRef lpNumberOfBytesWritten As Integer) As Integer
    Public Declare Function GetProcAddress Lib "kernel32" (ByVal hModule As Integer, ByVal lpProcName As String) As Integer
    Private Declare Function GetModuleHandle Lib "Kernel32" Alias "GetModuleHandleA" (ByVal lpModuleName As String) As Integer
    Public Declare Function CreateRemoteThread Lib "kernel32" (ByVal hProcess As Integer, ByVal lpThreadAttributes As Integer, ByVal dwStackSize As Integer, ByVal lpStartAddress As Integer, ByVal lpParameter As Integer, ByVal dwCreationFlags As Integer, ByRef lpThreadId As Integer) As Integer
    Public Declare Function OpenThread Lib "kernel32" (ByVal dwDesiredAccess As ThreadAccess, ByVal bInheritHandle As Boolean, ByVal dwThreadId As UInteger) As IntPtr
    Public Declare Function SuspendThread Lib "kernel32" (ByVal hThread As IntPtr) As UInteger
    Public Declare Function ResumeThread Lib "kernel32" (ByVal hThread As IntPtr) As Integer

    Public Sub Suspend(ByVal process As Process)
        For Each thread As ProcessThread In process.Threads
            Dim pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, False, CUInt(thread.Id))
            If pOpenThread = IntPtr.Zero Then
                Exit For
            End If
            SuspendThread(pOpenThread)
        Next thread
    End Sub
    Public Sub Resumes(ByVal process As Process)
        For Each thread As ProcessThread In process.Threads
            Dim pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, False, CUInt(thread.Id))
            If pOpenThread = IntPtr.Zero Then
                Exit For
            End If
            ResumeThread(pOpenThread)
        Next thread
    End Sub
    Dim FileName = ""
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        FileName = Application.StartupPath + "/AcitivateTest.exe"
        TextBox1.Text = FileName
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim DllPath As String = Application.StartupPath + "/UnmanagedDll.dll"
        If TextBox2.Text = "" Then Return
        Dim TargetHandle As IntPtr
        Try
            TargetHandle = System.Diagnostics.Process.GetProcessById(CInt(TextBox2.Text)).Handle
        Catch
        End Try
        If (TargetHandle.Equals(IntPtr.Zero)) Then
            MsgBox("打开进程失败.", vbMsgBoxSetForeground + vbSystemModal)
            Exit Sub
        End If
        Dim GetAdrOfLLBA As IntPtr = GetProcAddress(GetModuleHandle("Kernel32"), "LoadLibraryA")
        If (GetAdrOfLLBA.Equals(IntPtr.Zero)) Then
            MsgBox("获取LoadLibraryA失败.", vbMsgBoxSetForeground + vbSystemModal)
            Exit Sub
        End If

        Dim OperaChar As Byte() = System.Text.Encoding.Default.GetBytes(DllPath)

        Dim DllMemPathAdr = VirtualAllocEx(TargetHandle, 0&, &H64, MEM_COMMIT, PAGE_EXECUTE_READWRITE)
        If (DllMemPathAdr.Equals(IntPtr.Zero)) Then
            MsgBox("申请目标进程空间失败.", vbMsgBoxSetForeground + vbSystemModal)
            Exit Sub
        End If

        If (WriteProcessMemory(TargetHandle, DllMemPathAdr, OperaChar, OperaChar.Length, 0) = False) Then
            MsgBox("写入内存失败!")
            Exit Sub
        End If

        CreateRemoteThread(TargetHandle, 0, 0, GetAdrOfLLBA, DllMemPathAdr, 0, 0)
        MsgBox("注入成功!", vbMsgBoxSetForeground + vbSystemModal)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim DllPath As String = Application.StartupPath + "/UnmanagedDll.dll"


        Dim P As New Process
        With P.StartInfo
            .UseShellExecute = False
            .CreateNoWindow = False
            .RedirectStandardOutput = True
            .FileName = TextBox1.Text
            .Arguments = ""
        End With
        P.Start()
        'Suspend(P)

        Dim TargetHandle As IntPtr = P.Handle
        If (TargetHandle.Equals(IntPtr.Zero)) Then
            MsgBox("打开进程失败!", vbMsgBoxSetForeground + vbSystemModal)
            Exit Sub
        End If

        Dim GetAdrOfLLBA As IntPtr = GetProcAddress(GetModuleHandle("Kernel32"), "LoadLibraryA")
        If (GetAdrOfLLBA.Equals(IntPtr.Zero)) Then
            MsgBox("获取LoadLibraryA失败.", vbMsgBoxSetForeground + vbSystemModal)
            Exit Sub
        End If

        Dim OperaChar As Byte() = System.Text.Encoding.Default.GetBytes(DllPath)

        Dim DllMemPathAdr = VirtualAllocEx(TargetHandle, 0&, &H64, MEM_COMMIT, PAGE_EXECUTE_READWRITE)
        If (DllMemPathAdr.Equals(IntPtr.Zero)) Then
            MsgBox("申请目标进程空间失败.", vbMsgBoxSetForeground + vbSystemModal)
            Exit Sub
        End If

        If (WriteProcessMemory(TargetHandle, DllMemPathAdr, OperaChar, OperaChar.Length, 0) = False) Then
            MsgBox("写入内存失败!")
            Exit Sub
        End If

        CreateRemoteThread(TargetHandle, 0, 0, GetAdrOfLLBA, DllMemPathAdr, 0, 0)
        MsgBox("注入成功!", vbMsgBoxSetForeground + vbSystemModal)

        'Resumes(P)
    End Sub
End Class

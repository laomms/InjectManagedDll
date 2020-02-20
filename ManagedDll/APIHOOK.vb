Imports System.Runtime.InteropServices

Public Class APIHOOK

    '巨注意：有的该死的版本里面marshal的读写方法滴偏移量参数无效，自己改改代码，把偏移直接加到基地址里面，偏移量用0 。有兴趣可以用Reflector察看一下。

    '有的同志习惯于用lstrcpyn这个api来取数组地址，方法就是把参数1,2都设置为数组本身，其返回值就是数组的内存地址。

    '虽然这是一种取变量非托管内存指针的有效方式。但这存在一些问题，建议还是使用marshal类直接在非托管内存操作。



    <DllImport("Kernel32.dll")>
    Private Shared Function VirtualProtect(ByVal lpAddress As IntPtr, ByVal dwSize As Integer, ByVal flNewProtect As Integer, ByRef lpflOldProtect As Integer) As Boolean
    End Function



    <DllImport("Kernel32.dll")>
    Private Shared Function GetProcAddress(ByVal hModule As IntPtr, ByVal lpProcName As String) As IntPtr
    End Function



    Const PAGE_EXECUTE_READWRITE As Integer = &H40          '内存保护属性

    Private ProcAddress As IntPtr                           'api函数地址

    Private lpflOldProtect As Integer = 0                   '原始内存保护属性

    Private OldEntry As IntPtr = Marshal.AllocHGlobal(5)    '原始入口点数据

    Private NewEntry As IntPtr = Marshal.AllocHGlobal(5)    '新入口点数据

    Private _Installed As Boolean



    Public ReadOnly Property Installed() As Boolean

        Get

            Return _Installed

        End Get

    End Property



    Public Sub New()

    End Sub



    Public Sub New(ByVal ModuleName As String, ByVal ProcName As String, ByVal lpAddress As IntPtr)

        Install(ModuleName, ProcName, lpAddress)

    End Sub



    Public Function Install(ByVal ModuleName As String, ByVal ProcName As String, ByVal lpAddress As IntPtr) As Boolean

        '模块句柄  

        Dim hModule As IntPtr

        For Each md As ProcessModule In Process.GetCurrentProcess.Modules

            If md.ModuleName.ToLower = ModuleName.ToLower Then

                hModule = md.BaseAddress

                Exit For

            End If

        Next

        If hModule = IntPtr.Zero Then Return False

        '函数入口 

        ProcAddress = GetProcAddress(hModule, ProcName)

        If ProcAddress = IntPtr.Zero Then Return False

        '修改内存属性

        If Not VirtualProtect(ProcAddress, 1, PAGE_EXECUTE_READWRITE, lpflOldProtect) Then Return False

        '------------------------------------------在非托管内存构造数据---------------------------------------------

        '读原始5字节

        For i As Integer = 0 To 4

            Marshal.WriteByte(OldEntry, i, Marshal.ReadByte(ProcAddress, i))

        Next

        '构造新5字节

        'jmp

        Marshal.WriteByte(NewEntry, 0, &HE9)

        '新入口地址——我们的处理函数的非托管地址，jmp是相对地址

        Marshal.WriteInt32(NewEntry, 1, lpAddress.ToInt32 - ProcAddress.ToInt32 - 5)

        '---------------------------------------------数据构造完毕-----------------------------------------------------

        _Installed = True

        Return True

    End Function



    Public Sub UnHook()

        If Not _Installed Then Return

        For i As Integer = 0 To 4

            Marshal.WriteByte(ProcAddress, i, Marshal.ReadByte(OldEntry, i))

        Next

    End Sub



    Public Sub Hook()

        If Not _Installed Then Return

        For i As Integer = 0 To 4

            Marshal.WriteByte(ProcAddress, i, Marshal.ReadByte(NewEntry, i))

            'Debug.Print(Marshal.ReadByte(NewEntry, i) & " " & Marshal.ReadByte(OldEntry, i))

        Next

    End Sub



    Public Function Uninstall() As Boolean

        If Not _Installed Then Return True

        UnHook()

        VirtualProtect(ProcAddress, 1, lpflOldProtect, lpflOldProtect)

        _Installed = True

        Return True

    End Function





End Class
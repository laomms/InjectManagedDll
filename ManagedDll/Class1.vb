Imports System.IO
Imports System.Runtime.InteropServices

Public Class Class1

	<DllImport("wininet.dll")>
    Public Shared Function HttpOpenRequestW(hConnect As IntPtr, szVerb As IntPtr, szURI As IntPtr, szHttpVersion As IntPtr, szReferer As IntPtr, accetpType As IntPtr, dwflags As Integer, dwcontext As IntPtr) As IntPtr
    End Function
    <DllImport("wininet.dll", SetLastError:=True)>
    Public Shared Function InternetReadFile(ByVal hFile As IntPtr, ByVal lpBuffer As IntPtr, ByVal dwNumberOfBytesToRead As Integer, ByRef lpdwNumberOfBytesRead As Integer) As Boolean
    End Function

    Private MessageBoxW_Hook As New APIHOOK()
    Private MessageBoxA_Hook As New APIHOOK()
    Public Sub StartHook()
        Dim MessageBoxWHook = New DMessageBoxW(AddressOf MessageBoxW_Hooked)
        Dim MessageBoxAHook = New DMessageBoxA(AddressOf MessageBoxA_Hooked)
        MessageBoxW_Hook.Install("user32.dll", "MessageBoxW", Marshal.GetFunctionPointerForDelegate(MessageBoxWHook))
        MessageBoxA_Hook.Install("user32.dll", "MessageBoxA", Marshal.GetFunctionPointerForDelegate(MessageBoxAHook))
        MessageBoxW_Hook.Hook()
        MessageBoxA_Hook.Hook()
    End Sub

#Region "MessageBoxW"
    <DllImport("user32.dll", EntryPoint:="MessageBoxW", CharSet:=CharSet.Unicode)>
    Public Shared Function MessageBoxW(ByVal hWnd As Integer, ByVal text As String, ByVal caption As String, ByVal type As UInteger) As IntPtr
    End Function

    <UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet:=CharSet.Unicode)>
    Private Delegate Function DMessageBoxW(ByVal hWnd As Integer, ByVal text As String, ByVal caption As String, ByVal type As UInteger) As IntPtr
    Private Function MessageBoxW_Hooked(ByVal hWnd As Integer, ByVal text As String, ByVal caption As String, ByVal type As UInteger) As IntPtr
        MessageBoxA_Hook.UnHook()
        Return MessageBoxW(hWnd, "已注入-" & text, "已注入-" & caption, type)
    End Function

#End Region

#Region "MessageBoxA"
    <DllImport("user32.dll", EntryPoint:="MessageBoxA", CharSet:=CharSet.Ansi)>
    Public Shared Function MessageBoxA(ByVal hWnd As Integer, ByVal text As String, ByVal caption As String, ByVal type As UInteger) As IntPtr
    End Function
    <UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet:=CharSet.Ansi)>
    Private Delegate Function DMessageBoxA(ByVal hWnd As Integer, ByVal text As String, ByVal caption As String, ByVal type As UInteger) As IntPtr
    Private Function MessageBoxA_Hooked(ByVal hWnd As Integer, ByVal text As String, ByVal caption As String, ByVal type As UInteger) As IntPtr
        MessageBoxA_Hook.UnHook()
        Return MessageBoxA(hWnd, "已注入-" & text, "已注入-" & caption, type)
    End Function
#End Region

End Class

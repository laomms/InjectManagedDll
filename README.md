# InlineHook

VB.NET或C#通过dllinject将hookapi的dll远程注入目标程序(不通过easyhook控件实现)。   
由于目标进程不支持加载非托管dll.所以借助CLR通过非托管dll加载托管dll函数,然后注入非托管dll到目标进程. 
其实没啥意义,C++可以一步到位,但是对于.net的dll未免是个参考.

![image](https://github.com/laomms/InlineHook/blob/master/injectManagedDll.png)


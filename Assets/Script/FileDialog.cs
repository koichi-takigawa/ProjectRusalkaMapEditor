using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public class OpenFileName
{
    public int structSize = 0;
    public IntPtr dlgOwner = IntPtr.Zero;
    public IntPtr instance = IntPtr.Zero;
    public string filter = "Map Files (*.map)\0*.map\0All Files (*.*)\0*.*\0";
    public string customFilter = null;
    public int maxCustFilter = 0;
    public int filterIndex = 0;
    public string file = new string(new char[2048]); // 十分な長さを確保
    public int maxFile = 2048;
    public string fileTitle = new string(new char[1024]);
    public int maxFileTitle = 1024;
    public string initialDir = null;
    public string title = null;
    public int flags = 0;
    public short fileOffset = 0;
    public short fileExtension = 0;
    public string defExt = null;
    public IntPtr custData = IntPtr.Zero;
    public IntPtr hook = IntPtr.Zero;
    public string templateName = null;
    public IntPtr reservedPtr = IntPtr.Zero;
    public int reservedInt = 0;
    public int flagsEx = 0;

    public OpenFileName()
    {
        structSize = Marshal.SizeOf(this);
    }
}

public static class Dialog
{
    // ボタンの種類を表す定数
    public enum MessageBoxButtons : uint
    {
        OK               = 0x00000000,
        OKCancel         = 0x00000001,
        AbortRetryIgnore = 0x00000002,
        YesNoCancel      = 0x00000003,
        YesNo            = 0x00000004,
        RetryCancel      = 0x00000005
    }

    // アイコンの種類を表す定数
    public enum MessageBoxIcon : uint
    {
        None     = 0x00000000,
        Error    = 0x00000010,
        Warning  = 0x00000020,
        Question = 0x00000030
    }

    // 戻り値の定数
    public enum MessageBoxResult
    {
        IDOK     = 1,
        IDCANCEL = 2,
        IDABORT  = 3,
        IDRETRY  = 4,
        IDIGNORE = 5,
        IDYES    = 6,
        IDNO     = 7
    }

    // 開く用に追加
    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

    // 保存用に追加
    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool GetSaveFileName([In, Out] OpenFileName ofn);

    // メッセージボックス用に追加
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    // メッセージボックスのラッパー関数
    public static MessageBoxResult ShowMessageBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        uint type = (uint)buttons | (uint)icon;
        int result = MessageBox(IntPtr.Zero, text, caption, type);
        return (MessageBoxResult)result;
    }
}

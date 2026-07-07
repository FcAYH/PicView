namespace PicView.Core.WindowsNT;

public static class WindowHook
{
    public static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const uint WM_NCCALCSIZE = 0x0083;
        if (msg == WM_NCCALCSIZE)
        {
            // Return zero to prevent default margin calculation
            if (wParam != IntPtr.Zero)
            {
                handled = true;
            }
        }
        return IntPtr.Zero;
    } 
}
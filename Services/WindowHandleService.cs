using System;
using Microsoft.UI.Xaml;

namespace DevDeck.Services
{
    public sealed class WindowHandleService
    {
        public IntPtr WindowHandle { get; private set; } = IntPtr.Zero;

        public void RegisterWindow(Window window)
        {
            WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        }
    }
}

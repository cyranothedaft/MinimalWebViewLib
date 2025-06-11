using System;
using System.Reflection;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.Extensions.Logging;


namespace MinimalWebViewLib.Window;

partial class MinimalWindow {
   private delegate void WindowSizeCallbackDelegate(HWND hwnd, WPARAM wParam, int lParamLo, int lParamHi);


   public static MinimalWindow Create(string windowTitle, int width, int height, uint backgroundColor, ILogger? logger) {
      UiThreadSynchronizationContext? uiThreadSyncCtx = null;
      MinimalWindow window = new(logger);

      //  👇 this is *probably* okay, right?
      // ReSharper disable once AccessToModifiedClosure
      HWND hwnd = registerAndCreate(windowTitle, width, height, backgroundColor,
                                    (hwnd, msg, wParam, lParam) => WndProc(() => uiThreadSyncCtx,
                                                                           onResize: (hwnd1, wParam1, lparam_lo, lparam_hi) => {
                                                                                        logger?.LogTrace("[callback] MinimalWindow.onResize: {hwnd:x8} {wparam} {lparam_lo} {lparam_hi}", hwnd1.Value, wParam1.Value, lparam_lo, lparam_hi);
                                                                                        window.raiseSizeEvent(width: lparam_lo,
                                                                                                              height: lparam_hi);
                                                                                     },
                                                                           onClosing: () => {
                                                                                         logger?.LogTrace("[callback] MinimalWindow.onClosing");
                                                                                         window.raiseClosingEvent();
                                                                                         window.Close();
                                                                                      },
                                                                           hwnd, msg, wParam, lParam));

      if (hwnd.Value == 0)
         throw new Exception("hwnd not created");

      window._hwnd = hwnd;
      SynchronizationContext.SetSynchronizationContext(uiThreadSyncCtx = new UiThreadSynchronizationContext(hwnd)); // we want to minimize the time that uiThreadSyncCtx is non-null and not yet set as the synchronization context

      return window;
   }


   internal static void Show(HWND? hwnd, ILogger? logger) {
      const SHOW_WINDOW_CMD Mode = SHOW_WINDOW_CMD.SW_NORMAL;

      logger?.LogTrace("MinimalWindow.Show(hwnd: 0x{hwnd:x8})", hwnd.HasValue ? hwnd.Value.Value : "null");
      if (hwnd.HasValue) {
         logger?.LogDebug("Show - hwnd is specified, so showing window as [{mode}]", Mode);
         PInvoke.ShowWindow(hwnd.Value, Mode);
      }
   }


   internal static void ShowMessageBox(HWND? hwnd, string message, string caption, ILogger? logger) {
      logger?.LogTrace("MinimalWindow.ShowMessageBox(hwnd: 0x{hwnd:x8}, message: {message}, caption: {caption})", hwnd.HasValue ? hwnd.Value.Value : "null", message, caption);
      if (hwnd.HasValue)
         PInvoke.MessageBox(hwnd.Value, message, caption, MESSAGEBOX_STYLE.MB_OK);

      // var result = PInvoke.MessageBox(hwnd, "WebView2 runtime not installed.\r\n" +
      //                                       "Download and install:\r\n"           +
      //                                       "https://developer.microsoft.com/en-us/microsoft-edge/webview2?form=MA13LH",
      //                                 "Error", MESSAGEBOX_STYLE.MB_OK | MESSAGEBOX_STYLE.MB_ICONERROR);
      //
      // if (result == MESSAGEBOX_RESULT.IDYES) {
      //    //TODO: show message: download WV2 bootstrapper from https://go.microsoft.com/fwlink/p/?LinkId=2124703 and run it
      // }
   }


   internal static void Close(ILogger? logger) {
      logger?.LogTrace("MinimalWindow.Close");
      PInvoke.PostQuitMessage(0);
   }


   private static HWND registerAndCreate(string windowTitle, int width, int height, uint backgroundColor,
                                         WNDPROC wndProc) {
      unsafe {
         HBRUSH backgroundBrush = PInvoke.CreateSolidBrush(backgroundColor);
         if (backgroundBrush.IsNull) {
            // fallback to the system background color in case it fails
            backgroundBrush = (HBRUSH)(IntPtr)(SYS_COLOR_INDEX.COLOR_BACKGROUND + 1);
         }

         HINSTANCE hInstance = PInvoke.GetModuleHandle((char*)null);
         ushort classId = registerWindowClass(hInstance, backgroundBrush, windowTitle, wndProc);
         if (classId == 0)
            throw new Exception("class not registered");

         return createWindow(hInstance, classId, windowTitle, width, height);


         // unsafe
         static ushort registerWindowClass(HINSTANCE hInstance, HBRUSH backgroundBrush, string windowTitle,
                                           WNDPROC wndProc
         ) {
            fixed ( char* classNamePtr = windowTitle ) {
               WNDCLASSW wc = new()
                                 {
                                    lpfnWndProc   = wndProc,
                                    lpszClassName = classNamePtr,
                                    hInstance     = hInstance,
                                    hbrBackground = backgroundBrush,
                                    style         = WNDCLASS_STYLES.CS_VREDRAW | WNDCLASS_STYLES.CS_HREDRAW
                                 };
               return PInvoke.RegisterClass(wc);
            }
         }

         // unsafe
         static HWND createWindow(HINSTANCE hInstance, ushort classId, string windowTitle, int width, int height) {
            fixed ( char* windowNamePtr = $"{windowTitle} {Assembly.GetExecutingAssembly().GetName().Version}" ) {
               return PInvoke.CreateWindowEx(0,
                                             (char*)classId,
                                             windowNamePtr,
                                             WINDOW_STYLE.WS_OVERLAPPEDWINDOW,
                                             PInvoke.CW_USEDEFAULT, PInvoke.CW_USEDEFAULT, width, height,
                                             new HWND(),
                                             new HMENU(),
                                             hInstance,
                                             null);
            }
         }
      }
   }


   private static LRESULT WndProc(Func<UiThreadSynchronizationContext?> uiThreadSyncCtxFunc,
                                  WindowSizeCallbackDelegate onResize, Action onClosing,
                                  HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam) {
      switch (msg) {
         case PInvoke.WM_SIZE:
            onResize(hwnd, wParam, GetLowWord(lParam.Value), GetHighWord(lParam.Value));
            break;

         case Constants.WM_SYNCHRONIZATIONCONTEXT_WORK_AVAILABLE:
            uiThreadSyncCtxFunc()?.RunAvailableWorkOnCurrentThread();
            break;

         case PInvoke.WM_CLOSE:
            onClosing();
            break;
      }

      return PInvoke.DefWindowProc(hwnd, msg, wParam, lParam);
   }


   private static int GetLowWord(nint value) {
      uint xy = (uint)value;
      int x = unchecked( (short)xy );
      return x;
   }


   private static int GetHighWord(nint value) {
      uint xy = (uint)value;
      int y = unchecked( (short)(xy >> 16) );
      return y;
   }
}

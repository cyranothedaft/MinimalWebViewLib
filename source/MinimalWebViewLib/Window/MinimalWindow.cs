using System;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.Extensions.Logging;



namespace MinimalWebViewLib.Window;

public partial class MinimalWindow {
   private readonly ILogger? _logger;
   private readonly WNDPROC _wndProc;
   private HWND? _hwnd;

   // these are here so that the instances are owned by this object
   private UiThreadSynchronizationContext? _uiThreadSyncCtx;


   public delegate void SizeChangedEventDelegate(int width, int height);
   public event SizeChangedEventDelegate? SizeChanged;

   public event Action? Closing;


   internal HWND Handle => _hwnd!.Value;


   private MinimalWindow(ILogger? logger) {
      _logger  = logger;
      _wndProc = wndProc;
   }


   private LRESULT wndProc(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
      => WndProc(_uiThreadSyncCtx,
                 onResize: (hwnd1, wParam1, lparam_lo, lparam_hi) => {
                              _logger?.LogTrace("[callback] MinimalWindow.onResize: {hwnd:x8} {wparam} {lparam_lo} {lparam_hi}", hwnd1.Value, wParam1.Value, lparam_lo, lparam_hi);
                              raiseSizeEvent(width: lparam_lo,
                                             height: lparam_hi);
                           },
                 onClosing: () => {
                               _logger?.LogTrace("[callback] MinimalWindow.onClosing");
                               raiseClosingEvent();
                               Close();
                            },
                 hwnd, msg, wParam, lParam);


   public void Show()                                         => Show(_hwnd, _logger);
   public void ShowMessageBox(string message, string caption) => ShowMessageBox(_hwnd, message, caption, _logger);
   public void Close() => Close(_logger);


   private void raiseSizeEvent(int width, int height) => SizeChanged?.Invoke(width, height);
   private void raiseClosingEvent() => Closing?.Invoke();
}

using System;
using Windows.Win32.Foundation;
using Microsoft.Extensions.Logging;



namespace MinimalWebViewLib.Window;

public partial class MinimalWindow {
   private readonly ILogger? _logger;
   private HWND? _hwnd;

   public delegate void SizeChangedEventDelegate(int width, int height);
   public event SizeChangedEventDelegate? SizeChanged;

   public event Action? Closing;


   internal HWND Handle => _hwnd!.Value;


   private MinimalWindow(ILogger? logger) {
      _logger = logger;
   }


   public void Show()                                         => Show(_hwnd, _logger);
   public void ShowMessageBox(string message, string caption) => ShowMessageBox(_hwnd, message, caption, _logger);
   public void Close() => Close(_logger);


   private void raiseSizeEvent(int width, int height) => SizeChanged?.Invoke(width, height);
   private void raiseClosingEvent() => Closing?.Invoke();
}

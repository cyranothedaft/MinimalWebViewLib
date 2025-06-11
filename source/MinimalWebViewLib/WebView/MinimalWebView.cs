using System;
using System.Drawing;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Core;


namespace MinimalWebViewLib.WebView;

public partial class MinimalWebView {
   public delegate void HandleMessageFromWebViewDelegate(string webMessageReceived);


   private readonly ILogger? _logger;
   private CoreWebView2Controller? _controller;

   public event Action? Initialized;
   public event Action? PageLoaded;
   public event HandleMessageFromWebViewDelegate? MessageFromPage;



   private MinimalWebView(ILogger? logger) {
      _logger = logger;
   }


   public void LoadPage() {
      _logger?.LogTrace("MinimalWebView.LoadPage() (_controller is {nullOrNot})", _controller is null ? "null" : "not null");
      if (_controller is not null) {
         navigateWebView(_controller);
      }
   }


   public void SetSize(int width, int height) {
      _logger?.LogTrace("MinimalWebView.SetSize({width}, {height}) (_controller is {nullOrNot})", width, height, _controller is null ? "null" : "not null");
      if (_controller is not null) {
         Rectangle bounds = new(0, 0, width, height);
         _logger?.LogTrace("setting bounds to: {bounds}", bounds);
         _controller.Bounds = bounds;
      }
   }


   public async Task<string?> ExecuteScriptAsync(string javascript) {
      _logger?.LogTrace("MinimalWebView.ExecuteScriptAsync(javascript: [logged separately]) (_controller is {nullOrNot})", _controller is null ? "null--script will not execute" : "not null");
      _logger?.LogTrace("MinimalWebView.ExecuteScriptAsync javascript: {javascript}", javascript);
      if (_controller is not null) {
         _logger?.LogDebug("executing script:  {truncatedScript}", javascript.Replace('\r', ' ')
                                                                             .Replace('\n', ' ')
                                                                             .TruncateTo(60, " (...)"));
         // this will blow up if not run on the UI thread, so the SynchronizationContext needs to have been wired up correctly
         string? returnValueJs = await _controller.CoreWebView2.ExecuteScriptAsync(javascript);
         if (returnValueJs is not null)
            _logger?.LogDebug("script returned (javascript expression):  {returnValueJs}", returnValueJs);

         return returnValueJs;
      }
      return null;
   }


   private void initController(HWND hwnd) {
      // Start initializing WebView2 in a fire-and-forget manner. Errors will be handled in the initialization function
      _ = initControllerAsync(hwnd, 
                              onInitialized: controller => {
                                           _logger?.LogTrace("[callback] MinimalWebView.onInitialized");
                                           _controller = controller;
                                           raiseInitializedEvent();
                                        },
                              handleNavigationCompleted,
                              handleMessageFromWebView,
                              _logger);
   }


   private void handleNavigationCompleted() {
      _logger?.LogTrace("MinimalWebView.handleNavigationCompleted()");
      raisePageLoadedEvent();
   }


   private void handleMessageFromWebView(string webMessageReceived) {
      _logger?.LogTrace("MinimalWebView.handleMessageFromWebView({message})", webMessageReceived);
      raiseWebMessageEvent(webMessageReceived);
   }


   private void raiseInitializedEvent()              => Initialized?.Invoke();
   private void raisePageLoadedEvent()               => PageLoaded?.Invoke();
   private void raiseWebMessageEvent(string message) => MessageFromPage?.Invoke(message);
}


public static class StringExtensions {
   public static string TruncateTo(this string s, int count, string? suffixIfTruncated = null)
      => s.Length <= count
               ? s
               : (s[..count] + (suffixIfTruncated ?? string.Empty));
}

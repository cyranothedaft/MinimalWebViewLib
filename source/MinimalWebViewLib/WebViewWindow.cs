using System;
using Microsoft.Extensions.Logging;
using MinimalWebViewLib.WebView;
using MinimalWebViewLib.Window;


namespace MinimalWebViewLib;

public record WebViewWindow(MinimalWindow Window, MinimalWebView WebView) {
   public static WebViewWindow Create(string windowTitle, int windowWidth, int windowHeight, uint windowBackgroundColor,
                                      ILogger? windowLogger, ILogger? webViewLogger) {
      MinimalWindow window = MinimalWindow.Create(windowTitle, windowWidth, windowHeight, windowBackgroundColor, windowLogger);
      MinimalWebView webView = MinimalWebView.Init(window, webViewLogger);
      window.SizeChanged += webView.SetSize;

      return new WebViewWindow(window, webView);
   }

}

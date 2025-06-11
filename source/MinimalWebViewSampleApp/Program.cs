using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using MinimalWebViewLib;
using MinimalWebViewLib.Console;
using MinimalWebViewLib.WebView;
using MinimalWebViewLib.Window;


namespace MinimalWebViewSample.App;

public static class Program {
   private const string WindowTitle = "Minimal WebView Sample";
   private const int WindowWidth = 800;
   private const int WindowHeight = 600;
   private const uint BackgroundColor = 0x271811; // this is actually #111827, Windows uses BBGGRR

   private static readonly Random _rng = new Random();


   [STAThread]
   static int Main() {
#if DEBUG // By default GUI apps have no console. Open one to enable Console.WriteLine debugging 🤠
      MinimalConsole.OpenConsole();
#endif

      using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                                                                      builder.AddSimpleConsole(options => {
                                                                                                  options.IncludeScopes   = true;
                                                                                                  options.SingleLine      = true;
                                                                                                  options.TimestampFormat = "HH:mm:ss ";
                                                                                                  options.ColorBehavior   = LoggerColorBehavior.Enabled;
                                                                                               })
                                                                             .AddFilter("WM", LogLevel.Debug)
                                                                             .SetMinimumLevel(LogLevel.Trace));

      ILogger? appLogger = loggerFactory?.CreateLogger("app");
      ILogger? uiLogger = loggerFactory?.CreateLogger("UI");
      ILogger? messagePumpLogger = loggerFactory?.CreateLogger("WM");

      (MinimalWindow window, MinimalWebView webView) = WebViewWindow.Create(WindowTitle, WindowWidth, WindowHeight, BackgroundColor, 
                                                                            uiLogger, uiLogger);
      window.Closing          += () => appLogger?.LogInformation("===[ Window is closing ]===");
      webView.Initialized     += () => {
                                    appLogger?.LogInformation("===[ WebView is initialized ]===");
                                    webView.LoadPage();
                                 };
      webView.PageLoaded      += () => appLogger?.LogInformation("===[ WebView page is loaded ]===");
      webView.MessageFromPage += message => handleWebMessageAsync(message, exception => throw exception);
      window.Show();


      async void handleWebMessageAsync(string webMessage, Action<Exception> handleException) {
         try {
            appLogger?.LogInformation("===[ Web message: {msg} ]===", webMessage);

            switch (webMessage) {
               case "MsgBox":
                  window.ShowMessageBox("Here is a message box.", "Here is a message box caption.");
                  break;

               case "AlertScript":
                  await webView.ExecuteScriptAsync($"alert('Hi from the UI thread! I got a message from the browser: {webMessage}')");
                  break;

               case "FunctionWithArg":
                  await webView.ExecuteScriptAsync($"window.callMe('stuff')");
                  break;

               case "FunctionWithReturn":
                  string? returnValue = await webView.ExecuteScriptAsync($"window.iReturnSomething()");
                  window.ShowMessageBox($"Script returned value:  {returnValue ?? "(null)"}.", "👇 Check It 👇");
                  break;

               case "ReplaceHtml":
                  string html = $"<h1>NEW CONTENT!</h1><h5>Your new random number is: <b>{_rng.Next(1, 999)}</b></h5>";
                  await webView.ExecuteScriptAsync($"window.replaceHtml('{html}')");
                  break;
            }

         }
         catch (Exception exception) {
            handleException(exception);
         }
      }

      int exitCode = MessagePump.Run(messagePumpLogger);

#if DEBUG
      Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
      Console.WriteLine($"Ended with exit code {exitCode}.");
      Console.WriteLine("Press enter to exit.");
      Console.ReadLine();
#endif
      return exitCode;
   }
}

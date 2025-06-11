using System;
using Windows.Win32;


namespace MinimalWebViewLib.Console;

public static class MinimalConsole {
   public static void OpenConsole() {
      PInvoke.AllocConsole();
   }
}

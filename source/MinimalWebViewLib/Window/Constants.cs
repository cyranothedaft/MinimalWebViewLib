﻿using System;
using Windows.Win32;


namespace MinimalWebViewLib.Window;

internal static class Constants {
   public const uint WM_SYNCHRONIZATIONCONTEXT_WORK_AVAILABLE = PInvoke.WM_USER + 1;
}

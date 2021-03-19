﻿// ==========================================================================
//  Notifo.io
// ==========================================================================
//  Copyright (c) Sebastian Stehle
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Serilog;
using Serilog.Configuration;

namespace NotifoIO.SDK
{
    internal static class LoggerExtensions
    {
        public static LoggerConfiguration PlatformSink(this LoggerSinkConfiguration configuration) =>
            configuration.NSLog();
    }
}

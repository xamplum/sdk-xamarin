﻿// ==========================================================================
//  Notifo.io
// ==========================================================================
//  Copyright (c) Sebastian Stehle
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;

namespace Notifo.SDK.PushEventProvider
{
    /// <summary>
    /// Push events provider interface.
    /// </summary>
    public interface IPushEventsProvider
    {
        /// <summary>
        /// Event triggered when token is refreshed.
        /// </summary>
        event EventHandler<TokenRefreshEventArgs> OnTokenRefresh;

        /// <summary>
        /// Event triggered when a notification is received.
        /// </summary>
        event EventHandler<NotificationEventArgs> OnNotificationReceived;

        /// <summary>
        /// Event triggered when a notification is opened.
        /// </summary>
        event EventHandler<NotificationEventArgs> OnNotificationOpened;

        /// <summary>
        /// Push notification token.
        /// </summary>
        public string Token { get; }
    }
}

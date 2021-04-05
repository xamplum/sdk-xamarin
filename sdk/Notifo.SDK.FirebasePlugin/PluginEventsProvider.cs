﻿// ==========================================================================
//  Notifo.io
// ==========================================================================
//  Copyright (c) Sebastian Stehle
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using Notifo.SDK.PushEventProvider;
using Plugin.FirebasePushNotification;

namespace Notifo.SDK.FirebasePlugin
{
    internal class PluginEventsProvider : IPushEventsProvider
    {
        public event EventHandler<TokenRefreshEventArgs>? OnTokenRefresh;
        public event EventHandler<NotificationDataEventArgs>? OnNotificationReceived;
        public event EventHandler<NotificationResponseEventArgs>? OnNotificationOpened;

        public PluginEventsProvider()
        {
            CrossFirebasePushNotification.Current.OnTokenRefresh += FirebasePushNotification_OnTokenRefresh;
            CrossFirebasePushNotification.Current.OnNotificationReceived += FirebasePushNotification_OnNotificationReceived;
            CrossFirebasePushNotification.Current.OnNotificationOpened += FirebasePushNotification_OnNotificationOpened;
        }

        private void FirebasePushNotification_OnTokenRefresh(object source, FirebasePushNotificationTokenEventArgs e)
        {
            var args = new TokenRefreshEventArgs(e.Token);
            OnRefreshTokenEvent(args);
        }

        protected virtual void OnRefreshTokenEvent(TokenRefreshEventArgs args) =>
            OnTokenRefresh?.Invoke(this, args);

        private void FirebasePushNotification_OnNotificationReceived(object source, FirebasePushNotificationDataEventArgs e)
        {
            var args = new NotificationDataEventArgs(e.Data);
            OnNotificationReceivedEvent(args);
        }

        protected virtual void OnNotificationReceivedEvent(NotificationDataEventArgs args) =>
            OnNotificationReceived?.Invoke(this, args);

        private void FirebasePushNotification_OnNotificationOpened(object source, FirebasePushNotificationResponseEventArgs e)
        {
            var args = new NotificationResponseEventArgs(e.Data);
            OnNotificationOpenedEvent(args);
        }

        protected virtual void OnNotificationOpenedEvent(NotificationResponseEventArgs args) =>
            OnNotificationOpened?.Invoke(this, args);
    }
}

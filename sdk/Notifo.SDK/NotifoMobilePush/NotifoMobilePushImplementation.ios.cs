﻿// ==========================================================================
//  Notifo.io
// ==========================================================================
//  Copyright (c) Sebastian Stehle
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Caching.Memory;
using Notifo.SDK.Extensions;
using Notifo.SDK.PushEventProvider;
using Notifo.SDK.Resources;
using Serilog;
using UserNotifications;
using Xamarin.Essentials;

namespace Notifo.SDK.NotifoMobilePush
{
    internal partial class NotifoMobilePushImplementation : NSObject
    {
        private INotificationHandler? notificationHandler;
        public INotifoMobilePush SetNotificationHandler(INotificationHandler? notificationHandler)
        {
            this.notificationHandler = notificationHandler;

            return this;
        }

        private readonly IMemoryCache imageCache = new MemoryCache(new MemoryCacheOptions());

        public async Task DidReceiveNotificationRequestAsync(UNNotificationRequest request, UNMutableNotificationContent bestAttemptContent)
        {
            Log.Debug(Strings.ReceivedNotification, request.Content.UserInfo);

            var userInfo = request.Content.UserInfo.ToDictionary();
            var notification = new NotificationDto().FromDictionary(userInfo);

            if (!string.IsNullOrWhiteSpace(notification.TrackingUrl))
            {
                await TrackNotificationAsync(notification.Id, notification.TrackingUrl);
            }

            await EnrichNotificationContentAsync(bestAttemptContent, notification);
        }

        public async Task DidReceivePullRefreshRequestAsync(PullRefreshOptions? options = null)
        {
            options ??= new PullRefreshOptions();

            var notifications = await GetPendingNotificationsAsync(options.Take, options.Period);

            foreach (var notification in notifications)
            {
                if (options.RaiseEvent)
                {
                    var eventArgs = new NotificationEventArgs(notification);
                    OnReceived(eventArgs);
                }

                if (notification.Silent)
                {
                    continue;
                }

                if (options.PresentNotification)
                {
                    await ShowLocalNotificationAsync(notification);
                }
            }

            await TrackNotificationsAsync(notifications);
        }

        private void OnReceived(NotificationEventArgs eventArgs)
        {
            foreach (var handler in receivedNotificationEvents)
            {
                handler.Invoke(this, eventArgs);
            }
        }

        private async Task ShowLocalNotificationAsync(NotificationDto notification)
        {
            var content = new UNMutableNotificationContent();

            content = await EnrichNotificationContentAsync(content, notification);

            content.UserInfo = notification.ToDictionary().ToNSDictionary();

            var request = UNNotificationRequest.FromIdentifier(notification.Id.ToString(), content, trigger: null);
            UNUserNotificationCenter.Current.AddNotificationRequest(request, (error) =>
            {
                if (error != null)
                {
                    Log.Debug(error.LocalizedDescription);
                }
            });
        }

        private async Task<UNMutableNotificationContent> EnrichNotificationContentAsync(UNMutableNotificationContent content, NotificationDto notification)
        {
            if (!string.IsNullOrWhiteSpace(notification.Subject))
            {
                content.Title = notification.Subject;
            }

            if (!string.IsNullOrWhiteSpace(notification.Body))
            {
                content.Body = notification.Body;
            }

            string image = string.IsNullOrWhiteSpace(notification.ImageLarge) ? notification.ImageSmall : notification.ImageLarge;

            if (!string.IsNullOrWhiteSpace(image))
            {
                var imagePath = await GetImageAsync(image);
                if (!string.IsNullOrWhiteSpace(imagePath))
                {
                    var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(imagePath)}";
                    var attachementUrl = new NSUrl(uniqueName, NSFileManager.DefaultManager.GetTemporaryDirectory());

                    NSFileManager.DefaultManager.Copy(NSUrl.FromFilename(imagePath), attachementUrl, out var error);
                    if (error != null)
                    {
                        Log.Error(error.LocalizedDescription);
                    }

                    var attachement = UNNotificationAttachment.FromIdentifier(
                        Constants.ImageLargeKey,
                        attachementUrl,
                        new UNNotificationAttachmentOptions(),
                        out error);

                    if (error == null)
                    {
                        content.Attachments = new UNNotificationAttachment[] { attachement };
                    }
                    else
                    {
                        Log.Error(error.LocalizedDescription);
                    }
                }
            }

            var actions = new List<UNNotificationAction>();

            if (!string.IsNullOrWhiteSpace(notification.ConfirmUrl) &&
                !string.IsNullOrWhiteSpace(notification.ConfirmText) &&
                !notification.IsConfirmed)
            {
                var confirmAction = UNNotificationAction.FromIdentifier(
                    Constants.ConfirmAction,
                    notification.ConfirmText,
                    UNNotificationActionOptions.Foreground);

                actions.Add(confirmAction);
            }

            if (!string.IsNullOrWhiteSpace(notification.LinkUrl) &&
                !string.IsNullOrWhiteSpace(notification.LinkText))
            {
                var linkAction = UNNotificationAction.FromIdentifier(
                    Constants.LinkAction,
                    notification.LinkText,
                    UNNotificationActionOptions.Foreground);

                actions.Add(linkAction);
            }

            if (actions.Any())
            {
                var categoryId = Guid.NewGuid().ToString();

                var newCategory = UNNotificationCategory.FromIdentifier(
                    categoryId,
                    actions.ToArray(),
                    new string[] { },
                    UNNotificationCategoryOptions.None);

                var categories = new List<UNNotificationCategory>();

                var allCategories = await UNUserNotificationCenter.Current.GetNotificationCategoriesAsync();

                if (allCategories != null)
                {
                    foreach (UNNotificationCategory category in allCategories)
                    {
                        if (category.Identifier != categoryId)
                        {
                            categories.Add(category);
                        }
                    }

                    categories.Add(newCategory);
                }
                else
                {
                    categories.Add(newCategory);
                }

                UNUserNotificationCenter.Current.SetNotificationCategories(new NSSet<UNNotificationCategory>(categories.ToArray()));

                // without this call action buttons won't be added or updated
                _ = await UNUserNotificationCenter.Current.GetNotificationCategoriesAsync();

                content.CategoryIdentifier = categoryId;
            }

            if (content.Sound == null)
            {
                content.Sound = UNNotificationSound.Default;
            }

            notificationHandler?.OnBuildNotification(content, notification);

            return content;
        }

        public void DidReceiveNotificationResponse(UNUserNotificationCenter center, UNNotificationResponse response, Action completionHandler)
        {
            var userInfo = response.Notification.Request.Content.UserInfo.ToDictionary();

            object? value = default;
            switch (response.ActionIdentifier)
            {
                case Constants.ConfirmAction:
                    userInfo.TryGetValue(Constants.ConfirmUrlKey, out value);
                    break;
                case Constants.LinkAction:
                    userInfo.TryGetValue(Constants.LinkUrlKey, out value);
                    break;
            }

            var url = value?.ToString();
            if (!string.IsNullOrWhiteSpace(url))
            {
                Browser.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
            }
        }

        private async Task<string> GetImageAsync(string imageUrl)
        {
            try
            {
                if (imageCache.TryGetValue(imageUrl, out string imagePath) && File.Exists(imagePath))
                {
                    return imagePath;
                }

                var uri = new Uri(imageUrl);
                var imageByteArray = await httpClient.GetByteArrayAsync(uri);

                imagePath = Path.Combine(FileSystem.CacheDirectory, uri.Segments.Last());
                File.WriteAllBytes(imagePath, imageByteArray);

                imageCache.Set(imageUrl, imagePath);

                return imagePath;
            }
            catch (Exception ex)
            {
                Log.Error(Strings.DownloadImageError, ex);
                return string.Empty;
            }
        }
    }
}

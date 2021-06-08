// ==========================================================================
//  Notifo.io
// ==========================================================================
//  Copyright (c) Sebastian Stehle
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using UserNotifications;

namespace Notifo.SDK.Extensions
{
    /// <summary>
    /// UNNotificationRequest extensions.
    /// </summary>
    public static class UNNotificationRequestExtensions
    {
        /// <summary>
        /// Checks if the <see cref="UNNotificationRequest"/> is silent notification.
        /// </summary>
        /// <param name="request">The request that was received.</param>
        /// <returns>True if notification is silent.</returns>
        public static bool IsSilent(this UNNotificationRequest request)
        {
            var userInfo = request.Content.UserInfo.ToDictionary();
            var notification = new NotificationDto().FromDictionary(userInfo);

            return notification.Silent;
        }
    }
}

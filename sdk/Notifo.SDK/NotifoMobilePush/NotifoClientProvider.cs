﻿// ==========================================================================
//  Notifo.io
// ==========================================================================
//  Copyright (c) Sebastian Stehle
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Net.Http;

namespace Notifo.SDK
{
    internal class NotifoClientProvider
    {
        private bool rebuild;

        private string? apiKey;
        public string? ApiKey
        {
            get => apiKey;
            set
            {
                apiKey = value;
                clientBuilder.SetApiKey(apiKey);

                rebuild = true;
            }
        }

        private string apiUrl = "https://app.notifo.io";
        public string ApiUrl
        {
            get => apiUrl;
            set
            {
                apiUrl = value.TrimEnd('/');
                clientBuilder.SetApiUrl(apiUrl);

                rebuild = true;
            }
        }

        private INotifoClient? client;
        private INotifoClient Client
        {
            get
            {
                if (client == null || rebuild)
                {
                    rebuild = false;

                    httpClient.DefaultRequestHeaders.Clear();
                    client = clientBuilder.Build();
                }

                return client;
            }
        }

        public IMobilePushClient MobilePush => Client.MobilePush;
        public INotificationsClient Notifications => Client.Notifications;

        private readonly HttpClient httpClient;
        private readonly NotifoClientBuilder clientBuilder;

        public NotifoClientProvider(HttpClient httpClient)
        {
            this.httpClient = httpClient;

            clientBuilder = NotifoClientBuilder
                .Create()
                .SetClient(httpClient);
        }
    }
}

// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

#if WP7
extern alias Tasks;
using Tasks.System.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

using System;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    public static class IHttpClientExtensions
    {
        public static Task<IResponse> Post(this IHttpClient client, string url, Action<IRequest> prepareRequest)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            if (prepareRequest == null)
            {
                throw new ArgumentNullException("prepareRequest");
            }

            return client.Post(url, prepareRequest, postData: null);
        }
    }
}

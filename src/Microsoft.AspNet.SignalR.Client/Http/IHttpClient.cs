// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

#if WP7
extern alias Tasks;
using Tasks.System.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNet.SignalR.Client.Http
{
    /// <summary>
    /// A client that can make http request.
    /// </summary>
    public interface IHttpClient
    {
        /// <summary>
        /// Makes an asynchronous http GET request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Get", Justification = "Performs a GET request")]
        Task<IResponse> Get(string url, Action<IRequest> prepareRequest);

        /// <summary>
        /// Makes an asynchronous http POST request to the specified url.
        /// </summary>
        /// <param name="url">The url to send the request to.</param>
        /// <param name="prepareRequest">A callback that initializes the request with default values.</param>
        /// <param name="postData">form url encoded data.</param>
        /// <returns>A <see cref="T:Task{IResponse}"/>.</returns>
        Task<IResponse> Post(string url, Action<IRequest> prepareRequest, IDictionary<string, string> postData);
    }
}

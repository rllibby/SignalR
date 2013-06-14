// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

#if WP7
extern alias Tasks;
using Tasks.System.Threading;
using Tasks.System.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Microsoft.AspNet.SignalR.Client.Transports
{
    public interface IClientTransport : IDisposable
    {
        string Name { get; }
        bool SupportsKeepAlive { get; }

        Task<NegotiationResponse> Negotiate(IConnection connection);
        Task Start(IConnection connection, string data, CancellationToken disconnectToken);
        Task Send(IConnection connection, string data);
        void Abort(IConnection connection, TimeSpan timeout);

        void LostConnection(IConnection connection);
    }
}


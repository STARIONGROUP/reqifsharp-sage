// -------------------------------------------------------------------------------------------------
// <copyright file="StubHttpMessageHandler.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Knowledge.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A minimal, hand-rolled <see cref="HttpMessageHandler"/> stub - no real network calls in unit tests.
    /// </summary>
    public sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpRequestMessage, HttpResponseMessage>> responses = new();

        public int RequestCount { get; private set; }

        public void Enqueue(Func<HttpRequestMessage, HttpResponseMessage> respond)
        {
            this.responses.Enqueue(respond);
        }

        public void EnqueueSuccess(byte[] content)
        {
            this.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(content) });
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.RequestCount++;

            if (this.responses.Count == 0)
            {
                throw new InvalidOperationException("No more stubbed responses were enqueued.");
            }

            return Task.FromResult(this.responses.Dequeue()(request));
        }
    }
}

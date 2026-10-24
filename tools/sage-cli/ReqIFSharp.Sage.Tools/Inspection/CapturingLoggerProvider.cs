// -------------------------------------------------------------------------------------------------
// <copyright file="CapturingLoggerProvider.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Inspection
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Captures the warning and error log entries reqifsharp emits while deserializing, so they become READER findings.
    /// </summary>
    internal sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<(LogLevel Level, string Message)> entries = new();

        /// <summary>
        /// Gets the captured entries, in the order they were logged.
        /// </summary>
        internal IReadOnlyCollection<(LogLevel Level, string Message)> Entries => this.entries;

        /// <inheritdoc/>
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this.entries);

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        private sealed class CapturingLogger(ConcurrentQueue<(LogLevel Level, string Message)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (this.IsEnabled(logLevel))
                {
                    entries.Enqueue((logLevel, formatter(state, exception)));
                }
            }
        }
    }
}

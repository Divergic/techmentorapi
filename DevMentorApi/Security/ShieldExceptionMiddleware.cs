﻿namespace DevMentorApi.Security
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using EnsureThat;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using DevMentorApi.Core;
    using DevMentorApi.Properties;

    public class ShieldExceptionMiddleware
    {
        private readonly IResultExecutor _executor;
        private readonly ILogger<ShieldExceptionMiddleware> _logger;
        private readonly RequestDelegate _next;

        public ShieldExceptionMiddleware(
            RequestDelegate next,
            ILogger<ShieldExceptionMiddleware> logger,
            IResultExecutor executor)
        {
            Ensure.That(next, nameof(next)).IsNotNull();
            Ensure.That(logger, nameof(logger)).IsNotNull();
            Ensure.That(executor, nameof(executor)).IsNotNull();

            _next = next;
            _logger = logger;
            _executor = executor;
        }

        public async Task Invoke(HttpContext context)
        {
            Ensure.That(context, nameof(context)).IsNotNull();

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning(
                        default(EventId),
                        ex,
                        "Failed to shield exception as response headers have already been written.");

                    return;
                }

                var result = new ErrorMessageResult(
                    Resources.WebApi_ExceptionShieldMessage,
                    HttpStatusCode.InternalServerError);

                await _executor.Execute(context, result);
            }
        }
    }
}
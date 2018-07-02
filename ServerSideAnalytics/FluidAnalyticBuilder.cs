﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Maddalena;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace ServerSideAnalytics
{
    public class FluidAnalyticBuilder
    {
        private readonly IAnalyticStore _store;
        private List<Func<HttpContext, bool>> _exclude;
        private Func<IPAddress, CountryCode> _geoResolve;

        private static string UserIdentity(HttpContext context)
        {
            var user = context.User?.Identity?.Name;

            return string.IsNullOrWhiteSpace(user)
                ? (context.Request.Cookies.ContainsKey("ai_user")
                    ? context.Request.Cookies["ai_user"]
                    : context.Connection.Id)
                : user;
        }

        internal FluidAnalyticBuilder(IApplicationBuilder app, IAnalyticStore store)
        {
            _app = app;
            _store = store;
        }

        internal async Task Run(HttpContext context, Func<Task> next)
        {
            if (_exclude?.Any(x => x(context)) ?? false)
            {
                await next.Invoke();
                return;
            }

            var req = new WebRequest
            {
                Timestamp = DateTime.Now,
                Identity = UserIdentity(context),
                RemoteIpAddress = context.Connection.RemoteIpAddress.ToString(),
                Method = context.Request.Method,
                UserAgent = context.Request.Headers["User-Agent"],
                Path = context.Request.Path.Value,
                Country = _geoResolve?.Invoke(context.Connection.RemoteIpAddress) ?? CountryCode.World
            };

            await _store.AddAsync(req);
            await next.Invoke();
        }

        public FluidAnalyticBuilder Exclude(Func<HttpContext, bool> filter)
        {
            if(_exclude == null) _exclude = new List<Func<HttpContext, bool>>();
            _exclude.Add(filter);
            return this;
        }

        public FluidAnalyticBuilder Exclude(IPAddress ip) => Exclude(x => Equals(x.Connection.RemoteIpAddress, ip));

        public FluidAnalyticBuilder ExcludePath(string path) => Exclude(x => Equals(x.Request.Path.StartsWithSegments(path)));

        public FluidAnalyticBuilder LimitToPath(string path) => Exclude(x => !Equals(x.Request.Path.StartsWithSegments(path)));

        public FluidAnalyticBuilder ExcludeExtension(string extension) => Exclude(x => x.Request.Path.Value?.EndsWith(extension) ?? false);

        public FluidAnalyticBuilder ExcludeLoopBack() => Exclude(x => IPAddress.IsLoopback(x.Connection.RemoteIpAddress));

        public FluidAnalyticBuilder UseGeoIp(Func<IPAddress, CountryCode> geoResolve)
        {
            _geoResolve = geoResolve;
            return this;
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Serilog.Enrichers.AspnetcoreHttpcontext;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace SimpleApi
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                CreateWebHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Host terminated unexpectedly");
                Console.Write(ex.ToString());
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }


            //var name = Assembly.GetExecutingAssembly().GetName();
            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Debug()
            //    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            //    .MinimumLevel.Override("IdentityServer4", LogEventLevel.Information)
            //    .Enrich.FromLogContext()
            //    .Enrich.WithMachineName()
            //    .Enrich.WithProperty("Assembly", $"{name.Name}")
            //    .Enrich.WithProperty("Version", $"{name.Version}")
            //    .WriteTo.File(new RenderedCompactJsonFormatter(), @"C:\users\edahl\Source\Logs\SimpleApi.json")
            //    .CreateLogger();

            //try
            //{
            //    Log.Information("Starting web host");
            //    CreateWebHostBuilder(args).Build().Run();
            //    return 0;
            //}
            //catch (Exception ex)
            //{
            //    Log.Fatal(ex, "Host terminated unexpectedly");
            //    return 1;
            //}
            //finally
            //{
            //    Log.CloseAndFlush();
            //}
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseSerilog((provider, ContextBoundObject, loggerConfig) =>
                {
                    var name = Assembly.GetExecutingAssembly().GetName();
                    loggerConfig
                        .MinimumLevel.Debug()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .MinimumLevel.Override("IdentityServer4", LogEventLevel.Information)
                        .Enrich.WithAspnetcoreHttpcontext(provider, false, AddCustomContextInfo)
                        .Enrich.FromLogContext()
                        .Enrich.WithMachineName()
                        .Enrich.WithProperty("Assembly", $"{name.Name}")
                        .Enrich.WithProperty("Version", $"{name.Version}")                        
                        .WriteTo.File(new CompactJsonFormatter(), @"C:\users\edahl\Source\Logs\SimpleApi.json");
                });
        }



        public static void AddCustomContextInfo(IHttpContextAccessor ctx, LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            HttpContext context = ctx.HttpContext;
            if (context == null)
            {
                return;
            }
            var userInfo = context.Items[$"serilog-enrichers-aspnetcore-userinfo"] as UserInfo;
            if (userInfo == null)
            {
                var user = context.User.Identity;
                if (user == null || !user.IsAuthenticated) return;
                var i = 0;
                userInfo = new UserInfo
                {
                    Name = user.Name,
                    Claims = context.User.Claims.ToDictionary(x => $"{x.Type} ({i++})", y => y.Value)
                };
                context.Items[$"serilog-enrichers-aspnetcore-userinfo"] = userInfo;
            }

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserInfo", userInfo, true));
        }
    }
    public class UserInfo
    {
        public string Name { get; set; }
        public Dictionary<string, string> Claims { get; set; }
    }
}

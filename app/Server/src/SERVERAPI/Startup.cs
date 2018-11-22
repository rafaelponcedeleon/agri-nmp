﻿/*
 
 *
 
 *
 * OpenAPI spec version: v1
 * 
 * 
 */

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using SERVERAPI.Utility;
using SERVERAPI.Controllers;
using System.Globalization;
using Agri.Models.Settings;
using Agri.Data;
using Agri.Interfaces;
using Microsoft.EntityFrameworkCore;
using Agri.LegacyData.Models.Impl;
using System.IO;
using System.Data.SqlClient;

namespace SERVERAPI
{
    /// <summary>
    /// The application Startup class
    /// </summary>
    public class Startup
    {
        private readonly IHostingEnvironment _hostingEnv;

        public IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            _hostingEnv = env;

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                builder.AddUserSecrets<Startup>();
            }
            //else
            //{
            //    if (Directory.Exists("/etc/secret-volume"))
            //    {
            //        builder.AddJsonFile("/etc/secret-volume/agri-secret", true);
            //    }
            //    else
            //    {
            //        Console.WriteLine("/etc/secret-volume was NOT FOUND");
            //    }
            //}

            Configuration = builder.Build();

            //Console.WriteLine("Agri-secret");
            //Console.WriteLine(Configuration["database-name"]);
            //Console.WriteLine(Configuration["database-user"]);
            Console.WriteLine(Environment.GetEnvironmentVariable("pgsqluri") ?? "pgsqluri not found");
            Console.WriteLine(Environment.GetEnvironmentVariable("pgsqlpassword") ?? "pgsqlpassword not found");
            Console.WriteLine(Environment.GetEnvironmentVariable("pgsqlusername") ?? "pgsqlusername not found");
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddAuthorization();
            var agriConnectionString = GetConnectionString();
            //Creates the DbContext as a scoped Service
            services.AddDbContext<AgriConfigurationContext>(options =>
            {
                options.UseNpgsql(agriConnectionString, b => b.MigrationsAssembly("Agri.Data"));
            });
            //services.AddScoped<IConfigurationRepository>(provider => new ConfigurationRepository(agriConnectionString));
            services.AddScoped<IViewRenderService, ViewRenderService>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IConfiguration>(Configuration);
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
            services.AddTransient<AgriSeeder>();

            //// allow for large files to be uploaded
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 1073741824; // 1 GB
            });
            services.AddResponseCompression();
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = ".NMP.Session";
                options.IdleTimeout = TimeSpan.FromHours(4);
            });

            // Enable Node Services
            services.AddNodeServices();

            //// Add framework services.
            services.AddMvc()
                .AddJsonOptions(
                    opts =>
                    {
                        opts.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                        opts.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                        opts.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat;
                        opts.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
                        // ReferenceLoopHandling is set to Ignore to prevent JSON parser issues with the user / roles model.
                        opts.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    });

            services.AddScoped<SERVERAPI.Models.Impl.UserData>();
            services.AddScoped<SERVERAPI.Models.Impl.StaticData>();
            services.AddScoped<SERVERAPI.Models.Impl.BrowserData>();
            services.AddScoped<IAgriConfigurationRepository, StaticDataExtRepository>();
            services.AddOptions();
            //services.AddAutoMapper(typeof(Startup).Assembly);
            //services.AddScoped<SERVERAPI.Utility.CalculateNutrients>();
            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseMiddleware(typeof(ErrorHandlingMiddleware));
            var cultureInfo = new CultureInfo("en-US");
            cultureInfo.NumberFormat.CurrencySymbol = "$";

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseSession();
            app.UseResponseCompression();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }

        private string GetConnectionString()
        {
            if (_hostingEnv.IsDevelopment())
            {
                return Configuration["Agri:ConnectionString"];
            }
            else
            {
                var server = Environment.GetEnvironmentVariable("pgsqluri");
                var password = Environment.GetEnvironmentVariable("pgsqlpassword");
                var username = Environment.GetEnvironmentVariable("pgsqlusername");

                // Create a new SqlConnectionStringBuilder and
                // initialize it with a few name/value pairs.
                //SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();

                // Pass the SqlConnectionStringBuilder an existing 
                // connection string, and you can retrieve and
                // modify any of the elements.
                //return "Server=localhost;Database=AgriConfiguration;Username=username;Password=password";
                return $"Server={server};Database=sampledb;Username={username};Password={password}";

                // Now that the connection string has been parsed,
                // you can work with individual items.
                //builder["Server"] = server;
                //builder["Database"] = "sampledb";
                //builder["Username"] = username;
                //builder.Password = password;

                //Console.WriteLine("ConnectionString: " + builder.ConnectionString);
                //return builder.ConnectionString;
            }

            //throw new Exception("ConnectionString not found");
        }
    }    
}

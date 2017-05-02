﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Server.Core;
using server;
using IdSvrHost.Services;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Identity;
using IdSvrHost.Models;
using IdentityServer4.Quickstart.UI;
using Newtonsoft.Json;
using Server.Core.Models;
using Microsoft.AspNetCore.Mvc.Cors.Internal;
using server.Services;

namespace server
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
            }

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            AutoMapper.Mapper.Initialize(cfg => {
                cfg.CreateMap<Patient, PatientImportViewModel>();
            });

            var builder = services.AddIdentityServer()
                    .AddTemporarySigningCredential()
                    .AddInMemoryIdentityResources(Config.GetIdentityResources())
                    .AddInMemoryApiResources(Config.GetApiResources())
                    .AddInMemoryClients(Config.GetClients());

			builder.Services.Configure<MongoDbRepositoryConfiguration>(Configuration.GetSection("MongoDbRepository"));
			builder.Services.AddTransient<IUserRepository, UserRepository>();
            builder.Services.AddTransient<UserRepository>();
            builder.Services.AddTransient<IProfileService, ProfileService>();
            builder.Services.AddTransient<IResourceOwnerPasswordValidator, UserResourceOwnerPasswordValidator>();
            builder.Services.AddTransient<IPasswordHasher<User>, PasswordHasher<User>>();
			builder.Services.AddTransient<UserRepository>();
            builder.Services.AddTransient<PatientRepository>();
            builder.Services.AddTransient<CalendarEventRepository>();
            builder.Services.AddTransient<TeamRepository>();
            builder.Services.AddTransient<UserUtilService>();

			services.AddCors(options =>
                {
                    // this defines a CORS policy called "default"
                    options.AddPolicy("default", policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().AllowCredentials());
                });

            // Add framework services.
            services.AddMvc(options =>
            {
                options.Filters.Add(new CorsAuthorizationFilterFactory("default"));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddFile("Logs/myapp-{Date}.txt", LogLevel.Debug);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseCors("default");

            app.UseIdentityServer();

            app.UseStaticFiles();

            app.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
            {
                Authority = Config.ApiAuthority,
                AllowedScopes = { "api" },
                RequireHttpsMetadata = false,
            });

            app.UseMvcWithDefaultRoute();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;

namespace WebApp_OpenIDConnect_DotNet
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("config.json")
                .AddJsonFile("appsettings.json")
                .Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container.
            services.AddMvc();

            // Add Authentication services.
            services.AddAuthentication(sharedOptions => sharedOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // Add the console logger.
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            // Configure error handling middleware.
            app.UseExceptionHandler("/Home/Error");

            // Add static files to the request pipeline.
            app.UseStaticFiles();

            // Configure the OWIN pipeline to use cookie auth.
            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            // Configure the OWIN pipeline to use OpenID Connect auth.
            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
            {
                ClientId = Configuration["AzureAD:ClientId"],
                Authority = string.Format(CultureInfo.InvariantCulture, Configuration["AzureAd:AadInstance"], "common", "/v2.0"),
                ResponseType = OpenIdConnectResponseType.IdToken,
                PostLogoutRedirectUri = Configuration["AzureAd:PostLogoutRedirectUri"],
                Events = new OpenIdConnectEvents
                {
                    OnRemoteFailure = RemoteFailure,
                    OnTokenValidated = TokenValidated
                },
                TokenValidationParameters = new TokenValidationParameters
                {
                    // Instead of using the default validation (validating against
                    // a single issuer value, as we do in line of business apps), 
                    // we inject our own multitenant validation logic
                    ValidateIssuer = false
                }
            });

            // Configure MVC routes
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private Task TokenValidated(TokenValidatedContext context)
        {
            /* ---------------------
            // Replace this with your logic to validate the issuer/tenant
               ---------------------       
            // Retriever caller data from the incoming principal
            string issuer = context.SecurityToken.Issuer;
            string subject = context.SecurityToken.Subject;
            string tenantID = context.Ticket.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;

            // Build a dictionary of approved tenants
            IEnumerable<string> approvedTenantIds = new List<string>
            {
                "<Your tenantID>",
                "9188040d-6c67-4c5b-b112-36a304b66dad" // MSA Tenant
            };

            if (!approvedTenantIds.Contains(tenantID))
                throw new SecurityTokenValidationException();
              --------------------- */

            return Task.FromResult(0);
        }

        // Handle sign-in errors differently than generic errors.
        private Task RemoteFailure(FailureContext context)
        {
            context.HandleResponse();
            context.Response.Redirect("/Home/Error?message=" + context.Failure.Message);
            return Task.FromResult(0);
        }
    }
}

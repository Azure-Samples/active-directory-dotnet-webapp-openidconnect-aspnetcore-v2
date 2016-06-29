---
services: active-directory
platforms: dotnet
author: gsacavdm
---

# Integrating Azure AD (v2.0 endpoint) into an ASP.NET Core web app
This sample shows how to build a .Net MVC web application that uses OpenID Connect to sign-in users with Microsoft Accounts and accounts from multiple Azure Active Directory tenants, using the ASP.Net Core OpenID Connect middleware.

For more information about how the protocols work in this scenario and other scenarios, see [Authentication Scenarios for Azure AD](http://go.microsoft.com/fwlink/?LinkId=394414).

> This sample has finally been updated to ASP.NET Core RC2.  Looking for previous versions of this code sample? Check out the tags on the [releases](../../releases) GitHub page.

## How To Run This Sample

Getting started is simple!  To run this sample you will need:
- Install .NET Core for Windows by following the instructions at [dot.net/core](https://dot.net/core), which will include Visual Studio 2015 Update 3.
- An Internet connection
- An Azure subscription (a free trial is sufficient)

Every Azure subscription has an associated Azure Active Directory tenant.  If you don't already have an Azure subscription, you can get a free subscription by signing up at [https://azure.microsoft.com](https://azure.microsoft.com).  All of the Azure AD features used by this sample are available free of charge.

### Step 1:  Clone or download this repository

From your shell or command line:

`git clone https://github.com/Azure-Samples/active-directory-dotnet-webapp-openidconnect-aspnetcore-v2.git`

### Step 2:  Register the sample with Microsoft

1. Sign in to the [Application Registration Portal](https://apps.dev.microsoft.com) with either a personal or work or school Microsoft account.
2. Click on **Add an app**
3. **Enter a friendly name** for the application, for example "WebApp-OpenIDConnect-DotNet-v2" and click on **Create application**.
4. Click on **Add Platform** and click on the **Web** tile.
5. **Enter the redirect URI** for the sample, which is by default `https://localhost:44353/signin-oidc`.
6. Scroll to the bottom of the page and click **Save**

All done!  Before moving on to the next step, you need to find the Application ID of your application.

1. While still in the Application Registration portal, find the Application ID value and copy it to the clipboard.

### Step 3:  Configure the sample to use your Azure Active Directory tenant

1. Open the solution in Visual Studio 2015.
2. Open the `config.json` file.
3. Find the `Tenant` property and replace the value with your AAD tenant name.
4. Find the `ClientId` and replace the value with the Application ID from the Azure portal.
5. If you changed the base URL of the sample, find the app key `ida:PostLogoutRedirectUri` and replace the value with the new base URL of the sample.

### Step 4:  Run the sample

Clean the solution, rebuild the solution, and run it.

Click the sign-in link on the homepage of the application to sign-in.  On the Azure AD sign-in page, enter the name and password of a user account that is in your Azure AD tenant.

## About The Code

This sample shows how to use the OpenID Connect ASP.Net Core middleware to sign-in users with Microsoft Accounts and accounts from multiple Azure Active Directory tenants. The middleware is initialized in the `Startup.cs` file, by passing it the Application ID of the application and the URL of the Azure AD tenant where the application is registered.  The middleware then takes care of:
- Downloading the Azure AD metadata and finding the signing keys, and finding the issuer name for the tenant.
- Processing OpenID Connect sign-in responses by validating the signature and issuer in an incoming JWT, extracting the user's claims, and putting them on ClaimsPrincipal.Current.
- Integrating with the session cookie ASP.Net Core middleware to establish a session for the user.

This sample has been coded to support both Microsoft accounts and Azure Active Directory accounts. The code that enables this can be found in Startup.cs where you first need to disable issuer validation,
```C#
app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
{
  // (...)
  TokenValidationParameters = new TokenValidationParameters
  {
    ValidateIssuer = false
  }
});
```
And then leverage the OnTokenValidated notification to implement your own issuer validation logic depending on which tenants you want to support (any tenant, Microsoft Account + specific list of Azure AD, single Azure AD, just Microsoft Account, etc)
```C#
app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
{
  (...)
  Events = new OpenIdConnectEvents
  {
    OnTokenValidated = TokenValidated
  }
});
```

```C#
private Task TokenValidated(TokenValidatedContext context)
{
  // Example of how to validate for Microsoft Account + specific Azure AD tenant       
  string tenantID = context.Ticket.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;

  IEnumerable<string> approvedTenantIds = new List<string>
  {
    "<Your tenantID>",
    "9188040d-6c67-4c5b-b112-36a304b66dad" // MSA Tenant
  };
  if (!approvedTenantIds.Contains(tenantID))
    throw new SecurityTokenValidationException();

  return Task.FromResult(0);
}
```

You can trigger the middleware to send an OpenID Connect sign-in request by decorating a class or method with the `[Authorize]` attribute, or by issuing a challenge,
```C#
await HttpContext.Authentication.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
```
Similarly you can send a signout request,
```C#
await HttpContext.Authentication.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
```
When a user is signed out, they will be redirected to the `Post_Logout_Redirect_Uri` specified when the OpenID Connect middleware is initialized.

All of the middleware in this project is created as a part of the open source [Asp.Net Security](https://github.com/aspnet/Security) project.

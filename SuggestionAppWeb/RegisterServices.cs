﻿using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

namespace SuggestionAppWeb;

public static class RegisterServices
{
   public static void ConfigureServices(this WebApplicationBuilder builder)
   {
      builder.Services.AddRazorPages();
      builder.Services.AddServerSideBlazor().AddMicrosoftIdentityConsentHandler();
      builder.Services.AddMemoryCache();
      builder.Services.AddControllersWithViews().AddMicrosoftIdentityUI();

      builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAdB2C"));

      builder.Services.AddAuthorizationBuilder().AddPolicy("Admin", policy =>
      {
         policy.RequireClaim("jobTitle", "Admin");
      });

      builder.Services.AddSingleton<IDbConnection, DbConnection>();
      builder.Services.AddSingleton<ICategoryData, MongoCategoryData>();
      builder.Services.AddSingleton<IStatusData, MongoStatusData>();
      builder.Services.AddSingleton<ISuggestionData, MongoSuggestionData>();
      builder.Services.AddSingleton<IUserData, MongoUserData>();
   }
}
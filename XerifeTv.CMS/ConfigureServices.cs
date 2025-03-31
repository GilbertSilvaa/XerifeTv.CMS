﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using XerifeTv.CMS.Models.Abstractions.Interfaces;
using XerifeTv.CMS.Models.Abstractions.Services;
using XerifeTv.CMS.Models.Channel;
using XerifeTv.CMS.Models.Channel.Interfaces;
using XerifeTv.CMS.Models.Content;
using XerifeTv.CMS.Models.Content.Interfaces;
using XerifeTv.CMS.Models.Dashboard;
using XerifeTv.CMS.Models.Dashboard.Interfaces;
using XerifeTv.CMS.Models.Movie;
using XerifeTv.CMS.Models.Movie.Interfaces;
using XerifeTv.CMS.Models.Series;
using XerifeTv.CMS.Models.Series.Interfaces;
using XerifeTv.CMS.Models.User;
using XerifeTv.CMS.Models.User.Interfaces;

namespace XerifeTv.CMS;

public static class ConfigureServices
{
  public static IServiceCollection AddConfiguration(
    this IServiceCollection services, IConfiguration _configuration)
  {
    services
      .AddAuthAuthorization(_configuration)
      .AddRepositories()
      .AddServices()
      .AddSwagger();

    return services;
  }

  private static IServiceCollection AddRepositories(this IServiceCollection services)
  {
    services.AddScoped<IMovieRepository, MovieRepository>();
    services.AddScoped<ISeriesRepository, SeriesRepository>();
    services.AddScoped<IChannelRepository, ChannelRepository>();
    services.AddScoped<IUserRepository, UserRepository>();
    return services;
  }

  private static IServiceCollection AddServices(this IServiceCollection services)
  {
    services.AddScoped<IMovieService, MovieSevice>();
    services.AddScoped<ISeriesService, SeriesService>();
    services.AddScoped<IChannelService, ChannelService>();
    services.AddScoped<IDashboardService, DashboardService>();
    services.AddScoped<IContentService, ContentService>();
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<ITokenService, TokenService>();
    services.AddScoped<ICacheService, CacheService>();
    services.AddScoped<IStorageFilesService, StorageFilesService>();
    services.AddScoped<ISpreadsheetReaderService, SpreadsheetReaderService>();
    return services;
  }

  private static IServiceCollection AddAuthAuthorization(
    this IServiceCollection services, IConfiguration _configuration)
  {
    services.AddAuthentication(options =>
    {
      options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
      options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
      options.TokenValidationParameters = TokenService.GetTokenValidationParameters(_configuration);

      options.Events = new JwtBearerEvents
      {
        OnMessageReceived = context =>
        {
          context.Token = context.Request.Cookies["Token"];
          return Task.CompletedTask;
        }
      };
    });

    services.AddAuthorization();
    return services;
  }

  public static IServiceCollection AddSwagger(this IServiceCollection services)
  {
    services.AddSwaggerGen(options =>
    {
      options.SwaggerDoc("v1", new OpenApiInfo
      {
        Version = "v1",
        Title = "Content API",
        Description = "content API documentation"
      });
    });

    return services;
  }
}
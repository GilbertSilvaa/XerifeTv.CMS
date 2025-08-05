using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Abstractions.Services;
using XerifeTv.CMS.Modules.Authentication.Interfaces;
using XerifeTv.CMS.Modules.Authentication.Services;
using XerifeTv.CMS.Modules.BackgroundJobQueue;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.Channel;
using XerifeTv.CMS.Modules.Channel.Importers;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Content;
using XerifeTv.CMS.Modules.Content.Interfaces;
using XerifeTv.CMS.Modules.Dashboard;
using XerifeTv.CMS.Modules.Dashboard.Interfaces;
using XerifeTv.CMS.Modules.Integrations.Imdb.Services;
using XerifeTv.CMS.Modules.Movie;
using XerifeTv.CMS.Modules.Movie.Importers;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series;
using XerifeTv.CMS.Modules.Series.Importers;
using XerifeTv.CMS.Modules.Series.Interfaces;
using XerifeTv.CMS.Modules.User;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Modules.User.Services;

namespace XerifeTv.CMS.Shared.Extensions;

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
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IStorageFilesService, StorageFilesService>();
        services.AddScoped<ISpreadsheetReaderService, SpreadsheetReaderService>();
        services.AddScoped<IHashPassword, HashPassword>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ISpreadsheetBatchImporter<IMovieService>, MoviesSpreadsheetImporter>();
        services.AddScoped<ISpreadsheetBatchImporter<IChannelService>, ChannelsSpreadsheetImporter>();
		services.AddScoped<ISpreadsheetBatchImporter<ISeriesService>, SeriesSpreadsheetImporter>();
		services.AddScoped<IEpisodesImporter, EpisodesImdbImporter>();
        services.AddScoped<IImdbService, ImdbService>();
        services.AddScoped<IBackgroundJobQueueRepository, BackgroundJobQueueRepository>();
        services.AddScoped<IBackgroundJobQueueService, BackgroundJobQueueService>();
        services.AddHostedService<BackgroundJobQueueWorker>();
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
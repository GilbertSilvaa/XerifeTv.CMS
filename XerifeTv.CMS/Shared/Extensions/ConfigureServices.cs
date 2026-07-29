using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using MongoDB.Driver;
using XerifeTv.CMS.Modules.Abstractions.Interfaces;
using XerifeTv.CMS.Modules.Abstractions.Services;
using XerifeTv.CMS.Modules.AuditLog;
using XerifeTv.CMS.Modules.AuditLog.Interfaces;
using XerifeTv.CMS.Modules.Authentication.Interfaces;
using XerifeTv.CMS.Modules.Authentication.Services;
using XerifeTv.CMS.Modules.BackgroundJobQueue;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Interfaces;
using XerifeTv.CMS.Modules.BackgroundJobQueue.ProcessorStrategies;
using XerifeTv.CMS.Modules.BackgroundJobQueue.Services;
using XerifeTv.CMS.Modules.Channel;
using XerifeTv.CMS.Modules.Channel.Importers;
using XerifeTv.CMS.Modules.Channel.Interfaces;
using XerifeTv.CMS.Modules.Content;
using XerifeTv.CMS.Modules.Content.Interfaces;
using XerifeTv.CMS.Modules.Content.Services;
using XerifeTv.CMS.Modules.Dashboard;
using XerifeTv.CMS.Modules.Dashboard.Interfaces;
using XerifeTv.CMS.Modules.Franchise;
using XerifeTv.CMS.Modules.Franchise.Interfaces;
using XerifeTv.CMS.Modules.Integrations.Imdb.Services;
using XerifeTv.CMS.Modules.Integrations.Webhook;
using XerifeTv.CMS.Modules.Integrations.Webhook.Interfaces;
using XerifeTv.CMS.Modules.Media.Delivery;
using XerifeTv.CMS.Modules.Media.Delivery.Intefaces;
using XerifeTv.CMS.Modules.Media.Delivery.Services;
using XerifeTv.CMS.Modules.Media.Delivery.Services.MediaTokenStrategies;
using XerifeTv.CMS.Modules.Movie;
using XerifeTv.CMS.Modules.Movie.Importers;
using XerifeTv.CMS.Modules.Movie.Interfaces;
using XerifeTv.CMS.Modules.Series;
using XerifeTv.CMS.Modules.Series.Importers;
using XerifeTv.CMS.Modules.Series.Interfaces;
using XerifeTv.CMS.Modules.User;
using XerifeTv.CMS.Modules.User.Interfaces;
using XerifeTv.CMS.Modules.User.Services;
using XerifeTv.CMS.Shared.Database.MongoDB;

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
        services.AddSingleton<IMongoClient>(sp =>
        {
            var dbSettings = sp.GetRequiredService<IOptions<DBSettings>>().Value;
            return new MongoClient(dbSettings.ConnectionString);
        });

        services.AddScoped<IMovieRepository, MovieRepository>();
        services.AddScoped<ISeriesRepository, SeriesRepository>();
        services.AddScoped<IChannelRepository, ChannelRepository>();
        services.AddScoped<IFranchiseRepository, FranchiseRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWebhookRepository, WebhookRepository>();
        services.AddScoped<IMediaDeliveryProfileRepository, MediaDeliveryProfileRepository>();
        services.AddScoped<IContentSettingsRepository, ContentSettingsRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IMovieService, MovieService>();
        services.AddScoped<ISeriesService, SeriesService>();
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<IFranchiseService, FranchiseService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IContentV1Service, ContentV1Service>();
        services.AddScoped<IContentV2Service, ContentV2Service>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<ICacheService, CacheService>();
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
        services.AddScoped<ILoginStrategy, BasicLoginStrategy>();
        services.AddScoped<ILoginStrategy, GoogleLoginStrategy>();
        services.AddScoped<IWebhookService, WebhookService>();

        services.AddScoped<IMediaDeliveryProfileService, MediaDeliveryProfileService>();
        services.AddScoped<IMediaDeliveryUrlResolver, MediaDeliveryUrlResolver>();
        services.AddScoped<IMediaDeliveryTokenStrategy, MediaDeliveryTokenNoneStrategy>();
        services.AddScoped<IMediaDeliveryTokenStrategy, MediaDeliveryTokenStaticQueryStrategy>();
        services.AddScoped<IMediaDeliveryTokenStrategy, MediaDeliveryTokenSignedQueryStrategy>();

        services.AddScoped<IBackgroundJobProcessorStrategy, ImportSpreadsheetBackgroundJobProcessorStrategy>();
        services.AddScoped<IBackgroundJobProcessorStrategy, ImportEpisodesSeriesBackgroundJobProcessorStrategy>();
        services.AddScoped<IBackgroundJobProcessorStrategy, CalculateCategoryDistributionBackgroundJobProcessorStrategy>();
        services.AddScoped<IBackgroundJobProcessorStrategy, DispacthWebhooksBackgroundProcessorStrategy>();

        services.AddScoped<IAuditLogService, AuditLogService>();

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

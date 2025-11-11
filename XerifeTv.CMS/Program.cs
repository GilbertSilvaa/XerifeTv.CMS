using Scalar.AspNetCore;
using System.Globalization;
using XerifeTv.CMS.Shared.Database.MongoDB;
using XerifeTv.CMS.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

var defaultCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

builder.Services.AddControllersWithViews();

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAnyOrigin", builder =>
	{
		builder
			   .AllowAnyOrigin()
			   .AllowAnyMethod()
			   .AllowAnyHeader();
	});
});

builder.Services.Configure<DBSettings>(
	builder.Configuration.GetSection("MongoDBConfig"));

builder.Services.AddConfiguration(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.Urls.Add("http://*:80");
	app.UseHsts();
}

app.UseStatusCodePages(context =>
{
	var response = context.HttpContext.Response;

	if (response.StatusCode == 401)
	{
		var originalUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
		response.Redirect($"/Users/RefreshSession?successRedirectUrl={originalUrl}");
	}

	if (response.StatusCode == 403)
		response.Redirect("/Users/UserUnauthorized");

	if (response.StatusCode == 404)
		response.Redirect("/");

	return Task.CompletedTask;
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowAnyOrigin");

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger(options =>
{
	options.RouteTemplate = "openapi/{documentName}.json";
});

app.MapScalarApiReference("/Api", options =>
{
	options
		.WithTitle("Content API")
		.WithTheme(ScalarTheme.Moon)
		.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.Axios);
});

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
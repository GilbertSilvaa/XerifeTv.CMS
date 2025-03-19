using XerifeTv.CMS;
using XerifeTv.CMS.MongoDB;

var builder = WebApplication.CreateBuilder(args);

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

app.UseSwagger();

app.UseSwaggerUI(options =>
{
  options.SwaggerEndpoint("/swagger/v1/swagger.json", "Content API version 1");
  options.RoutePrefix = "Api";
});

app.MapControllerRoute(
  name: "default",
  pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

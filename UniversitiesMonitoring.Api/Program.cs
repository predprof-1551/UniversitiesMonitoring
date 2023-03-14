using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using UniversitiesMonitoring.Api;
using UniversitiesMonitoring.Api.Services;
using UniversitiesMonitoring.Api.WebSocket;
using UniversityMonitoring.Data;

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddDataContext(builder.Configuration);
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IUsersProvider, UsersProvider>()
    .AddScoped<IServicesProvider, ServicesProvider>()
    .AddScoped<IModeratorsProvider, ModeratorsProvider>()
    .AddScoped<IModulesProvider, ModulesProvider>()
    .AddSingleton<IWebSocketUpdateStateNotifier, WebSocketUpdateStateNotifier>()
    .AddSingleton<JwtGenerator>()
    .AddScoped<DatabaseSetupHelper>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;

    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidIssuer = "API_HOST",
        ValidateAudience = false,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
            Environment.GetEnvironmentVariable("JWT_SECRET") ??
            builder.Configuration["JwtSecret"])),
        ValidateIssuerSigningKey = true
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbHelper = scope.ServiceProvider.GetRequiredService<DatabaseSetupHelper>();
    await dbHelper.SetupDatabaseAsync();
}

if (!app.Environment.IsDevelopment()) app.UseHsts();
else
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = "/swagger";
    });
}

app.UseMiddleware<HandlingExceptionsMiddleware>();
app.UseHttpsRedirection();
app.UseWebSockets();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    "default",
    "/{controller}/{action}");

app.MapFallbackToFile("index.html");

app.Run();
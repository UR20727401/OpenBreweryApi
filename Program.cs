using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Models;
using OpenBreweryApi.Helpers;
using OpenBreweryApi.Interfaces;
using OpenBreweryApi.Middleware;
using OpenBreweryApi.Security;
using OpenBreweryApi.Services;
using OpenBreweryApi.Models.Settings;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Bind OpenBrewery settings
builder.Services.Configure<OpenBrewerySettings>(builder.Configuration.GetSection("OpenBrewery"));

 // Core services
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000;
});
builder.Services.AddHttpClient();
builder.Services.AddScoped<IBreweryService, BreweryService>();
builder.Services.AddScoped<IOpenBreweryClient, OpenBreweryClient>(); 

// Register AsyncLockProvider for concurrency control
builder.Services.AddSingleton<AsyncLockProvider>();
builder.Services.AddSingleton<OpenBreweryApi.Security.IApiKeyProvider, OpenBreweryApi.Security.EnvApiKeyProvider>();

// Authentication (simple API Key example) and Authorization
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        "ApiKey",
        options => { });

builder.Services.AddAuthorization();    

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(static options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "OpenBreweryApi", Version = "v1" });

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Name = "X-API-KEY",
        Description = "API key required. Add header: X-API-KEY: {key}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<ErrorHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "OpenBreweryApi v1");

        options.RoutePrefix = "swagger";
    });
}

app.MapControllers();

app.Run();
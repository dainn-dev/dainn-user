using System.Reflection;
using DainnUser.Api.Extensions;
using DainnUser.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DainnUser API",
        Version = "v1",
        Description = "User management API with authentication and authorization features",
        Contact = new OpenApiContact
        {
            Name = "DainnUser",
            Url = new Uri("https://github.com/hahndang/DainnUser")
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Add JWT Bearer security definition
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    // Add security requirement
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add DainnUser services with simplified registration
builder.Services.AddDainnUser(builder.Configuration, options =>
{
    options.EnableSocialLogin = false;
    options.EnableTwoFactor = false;
    options.RequireEmailVerification = true;
    options.EnableAccountLockout = true;
    options.EnableSessionManagement = true;
    options.EnableActivityLogging = true;
});

// Wire JWT bearer authentication using DainnUser's JWT options
builder.Services.AddDainnUserJwtAuthentication(builder.Configuration);

// Add CORS (configure as needed)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Add DainnUser middleware
app.UseDainnUser();

app.MapControllers();

// OpenAPI YAML export endpoint
app.MapGet("/openapi/v1.yaml", async (ISwaggerProvider swaggerProvider) =>
{
    var openApiDocument = swaggerProvider.GetSwagger("v1");
    var yaml = openApiDocument.SerializeAsYaml(OpenApiSpecVersion.OpenApi3_0);
    return Results.Content(yaml, "text/yaml");
});

app.Run();

/// <summary>
/// Public partial Program class for integration testing with WebApplicationFactory.
/// </summary>
public partial class Program { }

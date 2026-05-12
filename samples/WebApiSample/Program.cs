using DainnUser.Infrastructure;
using DainnUser.Api.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add DainnUser services
builder.Services.AddDainnUser(builder.Configuration);

// Add JWT authentication
builder.Services.AddDainnUserJwtAuthentication(builder.Configuration);

// Add controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DainnUser.Infrastructure.Data.DainnUserDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

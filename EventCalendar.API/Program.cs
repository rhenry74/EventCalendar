using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EventCalendar.API.Middleware;
using EventCalendar.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVite", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Location");
    });
});

// Add authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "EventCalendar",
            ValidAudience = "EventCalendarClients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ReplaceWithSecureLongSecretKey12345!"))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<EventRepository>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddSingleton<JsonFileLockService<User>>(sp => new JsonFileLockService<User>(Path.Combine(Directory.GetCurrentDirectory(), "Data", JsonFilePaths.UsersFile)));
builder.Services.AddSingleton<JsonFileLockService<Event>>(sp => new JsonFileLockService<Event>(Path.Combine(Directory.GetCurrentDirectory(), "Data", JsonFilePaths.EventsFile)));

var app = builder.Build();

app.UseCors("AllowVite");

// Place JWT auth before authorization
app.UseAuthentication();
app.UseMiddleware<JwtAuthMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TripPacking.BackgroundServices;
using TripPacking.Config;
using TripPacking.Data;
using TripPacking.Mappers;
using TripPacking.Middleware;
using TripPacking.Repositories;
using TripPacking.Services;

var builder = WebApplication.CreateBuilder(args);

var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "trippacking";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "root";
var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};Pooling=true;Max Pool Size=100;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
if (!string.IsNullOrEmpty(jwtSecret)) jwtSettings.SecretKey = jwtSecret;
if (!string.IsNullOrEmpty(jwtIssuer)) jwtSettings.Issuer = jwtIssuer;
if (!string.IsNullOrEmpty(jwtAudience)) jwtSettings.Audience = jwtAudience;

builder.Services.Configure<JwtSettings>(options =>
{
    options.SecretKey = jwtSettings.SecretKey;
    options.Issuer = jwtSettings.Issuer;
    options.Audience = jwtSettings.Audience;
    options.ExpirationMinutes = jwtSettings.ExpirationMinutes;
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TripPacking API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<ITripMemberRepository, TripMemberRepository>();
builder.Services.AddScoped<IPackingCategoryRepository, PackingCategoryRepository>();
builder.Services.AddScoped<IPackingItemRepository, PackingItemRepository>();
builder.Services.AddScoped<IPackingTemplateRepository, PackingTemplateRepository>();
builder.Services.AddScoped<IInvitationRepository, InvitationRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<ITripMemberService, TripMemberService>();
builder.Services.AddScoped<IPackingCategoryService, PackingCategoryService>();
builder.Services.AddScoped<IPackingItemService, PackingItemService>();
builder.Services.AddScoped<IPackingTemplateService, PackingTemplateService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<IInvitationService, InvitationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddHostedService<InvitationCleanupService>();

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var pending = db.Database.GetPendingMigrations().ToList();
        if (pending.Any())
        {
            db.Database.Migrate();
            logger.LogInformation("Database migration completed successfully");
        }
        else
        {
            db.Database.EnsureCreated();
            logger.LogInformation("Database EnsureCreated completed successfully");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed");
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseExceptionMiddleware();
app.UseJwtMiddleware();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => new { Status = "Healthy", Timestamp = DateTime.UtcNow })
    .WithName("HealthCheck");

app.Run();

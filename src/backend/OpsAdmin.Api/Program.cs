using System.Text;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpsAdmin.Api.Data;
using OpsAdmin.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 固定后端监听端口为 http://localhost:5000
builder.WebHost.UseUrls("http://localhost:5000");

// 1. Add Services to DI container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT Security Definition
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OpsAdmin Enterprise API", Version = "v1", Description = "基于.NET 8.0 + Redis + Hangfire 的企业运维管理平台 RESTful Web API" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
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
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// DbContext (MySQL 8.0)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=localhost;Port=3306;Database=opsadmin_db;Uid=root;Pwd=12345678;CharSet=utf8mb4;";

builder.Services.AddDbContext<OpsDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Hangfire with Redis Storage (127.0.0.1:6379, password=cxz@168)
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "127.0.0.1:6379,password=cxz@168,connectTimeout=5000,allowAdmin=true";
try
{
    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseRedisStorage(redisConnection, new RedisStorageOptions
        {
            Prefix = "opsadmin:hangfire:"
        }));
    builder.Services.AddHangfireServer();
}
catch (Exception ex)
{
    Console.WriteLine("Hangfire Redis 存储配置捕获警告: " + ex.Message);
}

// Jwt Configuration
var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "OpsAdminPlatformVeryLongSuperSecretKey2026Net8Vue3!@#$";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "OpsAdminServer";
var audience = builder.Configuration["Jwt:Audience"] ?? "OpsAdminClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// Custom Services
builder.Services.AddScoped<JwtService>();
builder.Services.AddHostedService<AlarmMonitoringBackgroundService>();

// CORS policy for Vue frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Auto seed database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OpsDbContext>();
    try
    {
        DbInitializer.Initialize(db);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "MySQL 数据库建表初始化过程中捕获异常，请核对 MySQL 数据库服务是否已在 localhost:3306 启动");
    }
}

// 2. Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

try
{
    app.UseHangfireDashboard("/hangfire");
    RecurringJob.AddOrUpdate("alarm-auto-monitoring-job", () => Console.WriteLine("[Hangfire 定时轮询引擎] 基于 Redis 内存介质执行自动化设备指标监控评估..."), Cron.Minutely);
}
catch (Exception ex)
{
    Console.WriteLine("Hangfire Dashboard 挂载提示: " + ex.Message);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MHStore.Repositories.Data;
using System.Text;
using DotNetEnv;
using AdminStatsServiceInterface = MHStore.Services.AdminStatsService.IService;
using AdminStatsServiceImplementation = MHStore.Services.AdminStatsService.Service;
using AccountServiceInterface = MHStore.Services.AccountService.IService;
using AccountServiceImplementation = MHStore.Services.AccountService.Service;
using JwtOptions = MHStore.Services.AccountService.JwtOptions;
using OrderServiceInterface = MHStore.Services.OrderService.IService;
using OrderServiceImplementation = MHStore.Services.OrderService.Service;
using MHStore.Services.PaymentService;
using PaymentServiceInterface = MHStore.Services.PaymentService.IService;
using PaymentServiceImplementation = MHStore.Services.PaymentService.Service;
using VietMapOptions = MHStore.Services.AddressService.VietMapOptions;
using AddressServiceInterface = MHStore.Services.AddressService.IService;
using AddressServiceImplementation = MHStore.Services.AddressService.Service;
using MHStore.API;
using ProductServiceInterface = MHStore.Services.ProductService.IService;
using ProductServiceImplementation = MHStore.Services.ProductService.Service;
using CategoryServiceInterface = MHStore.Services.CategoryService.IService;
using CategoryServiceImplementation = MHStore.Services.CategoryService.Service;

Env.Load();

var aspnetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", aspnetCoreEnv);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));
builder.Services.Configure<SePayOptions>(builder.Configuration.GetSection("SePay"));
builder.Services.Configure<VietMapOptions>(builder.Configuration.GetSection("VietMap"));

var jwtOptions = builder.Configuration.GetSection("JwtOptions").Get<JwtOptions>() ?? new JwtOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// Services
builder.Services.AddScoped<AdminStatsServiceInterface, AdminStatsServiceImplementation>();
builder.Services.AddScoped<AccountServiceInterface, AccountServiceImplementation>();
builder.Services.AddScoped<ProductServiceInterface, ProductServiceImplementation>();
builder.Services.AddScoped<CategoryServiceInterface, CategoryServiceImplementation>();
builder.Services.AddScoped<OrderServiceInterface, OrderServiceImplementation>();
builder.Services.AddScoped<PaymentServiceInterface, PaymentServiceImplementation>();
builder.Services.AddHttpClient<AddressServiceInterface, AddressServiceImplementation>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "https://fe-mhstore.vercel.app"
            )
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MHStore.API",
        Version = "v1"
    });

    options.CustomSchemaIds(type => type.FullName?.Replace('+', '.') ?? type.Name);
});

var app = builder.Build();

// Swagger (bật cả Production)
app.UseSwagger();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MHStore.API v1");
});

// Root endpoint để test Render
app.MapGet("/", () => "MHStore Backend is running");

/*
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
*/

app.UseCors("AllowFrontend");
// app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

/*
// Tự động chạy Migration khi khởi động
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await context.Database.MigrateAsync();
        await DevelopmentDataSeeder.SeedAsync(context, app.Configuration, app.Environment);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Lỗi khi cập nhật Database: {ex.Message}");
    }
}
*/

app.Run();
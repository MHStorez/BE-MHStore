using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MHStore.Repositories.Data;
using System.Text;
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
using ProductServiceInterface = MHStore.Services.ProductService.IService;
using ProductServiceImplementation = MHStore.Services.ProductService.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));
builder.Services.Configure<SePayOptions>(builder.Configuration.GetSection("SePay"));
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

// Tầng Service gọi trực tiếp AppDbContext
builder.Services.AddScoped<AdminStatsServiceInterface, AdminStatsServiceImplementation>();
builder.Services.AddScoped<AccountServiceInterface, AccountServiceImplementation>();
builder.Services.AddScoped<ProductServiceInterface, ProductServiceImplementation>();
builder.Services.AddScoped<OrderServiceInterface, OrderServiceImplementation>();
builder.Services.AddScoped<PaymentServiceInterface, PaymentServiceImplementation>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.FullName?.Replace('+', '.') ?? type.Name);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Tự động chạy Migration khi khởi động
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try 
    {
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Lỗi khi cập nhật Database: {ex.Message}");
    }
}

app.Run();

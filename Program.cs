using HMS_360_PMS;
using HMS_360_PMS.DAL_Layers.KOT;
using HMS_360_PMS.DAL_Layers.POS;
using HMS_360_PMS.Helper.Security;
using HMS_360_PMS.HMS_360_PMS.API.MIddlewares;
using HMS_360_PMS.HMS_360_PMS.Infrastructure;
using HMS_360_PMS.HMS_360_PMS.Infrastructure.KOT;
using HMS_360_PMS.HMS_360_PMS.Infrastructure.POS;
using HMS_360_PMS.HMS_360_PMS.ServiceLayer.KOT.Interfaces;
using HMS_360_PMS.HMS_360_PMS.ServiceLayer.KOT.Services;
using HMS_360_PMS.HMS_360_PMS.ServiceLayer.POS.Interfaces;
using HMS_360_PMS.HMS_360_PMS.ServiceLayer.POS.Services;
using HMS_360_PMS.ProjectInfrastructure.GeneralSettings;
using HMS_360_PMS.ProjectInfrastructure.KOT;
using HMS_360_PMS.ProjectInfrastructure.Master;
using HMS_360_PMS.ProjectInfrastructure.Payment;
using HMS_360_PMS.ProjectServiceLayer.GeneralSettings.Interfaces;
using HMS_360_PMS.ProjectServiceLayer.GeneralSettings.Services;
using HMS_360_PMS.ProjectServiceLayer.KOT.Interfaces;
using HMS_360_PMS.ProjectServiceLayer.KOT.Services.SMSEmailSender;
using HMS_360_PMS.ProjectServiceLayer.Master.Interfaces;
using HMS_360_PMS.ProjectServiceLayer.Master.Services;
using HMS_360_PMS.ProjectServiceLayer.Payment.Interface;
using HMS_360_PMS.ProjectServiceLayer.Payment.Services;
using HMS_360_PMS.Services_Layers.KOT.Interface;
using HMS_360_PMS.Services_Layers.KOT.Service;
using HMS_360_PMS.Services_Layers.POS.Interfaces;
using HMS_360_PMS.Services_Layers.POS.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

//-----------------------------------------------------//

// ==============================
// 🔥 SERILOG CONFIGURATION
// ==============================

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Async(a => a.File("Logs/log-.txt",
        rollingInterval: RollingInterval.Day))
    .CreateLogger();

builder.Host.UseSerilog();

//-----------------------------------------------------//

// Swagger (Development only)
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

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

// =========================
// 🔐 JWT CONFIGURATION
// =========================

var jwtKey = builder.Configuration["Jwt:Key"]
?? throw new InvalidOperationException("JWT Key is not configured.");
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    //options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

//-----------------------------------------------------//


// ========================
// =
// 📦 Dependency Injection
// Add services to the container.
// =========================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<JwtTokenService>();

// Repository
builder.Services.AddScoped<IPOS_Repository, POS_DAL>();
builder.Services.AddScoped<IPOSReports_Repository, POSReports_DAL>();
builder.Services.AddScoped<IKOT_Repository, KOT_DAL>();
builder.Services.AddScoped<IKOTDisplay_Repository, KOTDisplay_DAL>();
builder.Services.AddScoped<IKOTPickUp_Repository, KOTPickUp_DAL>();
builder.Services.AddScoped<IBasicSettingsManager, GeneralManager_DAL>();
builder.Services.AddScoped<IDashboardManager, GeneralManager_DAL>();
builder.Services.AddScoped<IPhonePeDQR_Repository, PhonePeDQRDevice_DAL>();
builder.Services.AddScoped<IMaster_Repository, Master_DAL>();
builder.Services.AddScoped<IUtilitySetting_Repository, UtilitySetting_DAL>();
builder.Services.AddScoped<IUserAccess_Repository, UserAccess_DAL>();

// DALs
builder.Services.AddScoped<POS_DAL>();
builder.Services.AddScoped<KotBillSettlement_DAL>();
builder.Services.AddScoped<NCKotBillSettlement_DAL>();
builder.Services.AddScoped<POSReports_DAL>();
builder.Services.AddScoped<KOT_DAL>();
builder.Services.AddScoped<KOTDisplay_DAL>();
builder.Services.AddScoped<KOTPickUp_DAL>();
builder.Services.AddScoped<GeneralManager_DAL>();
builder.Services.AddScoped<PhonePeDQRDevice_DAL>();
builder.Services.AddScoped<Master_DAL>();
builder.Services.AddScoped<UtilitySetting_DAL>();
builder.Services.AddScoped<UserAccess_DAL>();

// Services
builder.Services.AddHttpClient();

builder.Services.AddScoped<IPOS_Services, POS_Service>();
builder.Services.AddScoped<IPOSReports_Service, POSReports_Service>();
builder.Services.AddScoped<IKOT_Service, KOT_Service>();
builder.Services.AddScoped<IKOTDisplay_Service, KOTDisplay_Service>();
builder.Services.AddScoped<IKOTPickUp_Service, KOTPickUp_Service>();
builder.Services.AddScoped<IGraceTimeValidate_Service, GraceTimeValidate>();
builder.Services.AddScoped<IGeneralManager_Service, GeneralManager_Service>();
builder.Services.AddScoped<IPhonePeDQR_Service, PhonePeDQRDevice_Service>();
builder.Services.AddScoped<IMaster_Service, Master_Service>();
builder.Services.AddScoped<IUtilitySetting_Service, UtilitySetting_Service>();
builder.Services.AddScoped<IUserAccess_Service, UserAccess_Service>();

// appcongifuration
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<PhonePeSettings>(
    builder.Configuration.GetSection("PhonePe"));


builder.Services.ConfigureHttpJsonOptions(options =>
{
    //options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNamingPolicy = null;
});

//-----------------------------------------------------//

// =========================
// 🌍 CORS (For React)
// =========================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        policy =>
        {
            //policy.WithOrigins("http://localhost:5173") // React URL
            ////policy.WithOrigins("https://poswebsite.cogwave.in") // React URL
            //      .AllowAnyHeader()
            //      .AllowAnyMethod()
            //      .AllowCredentials();

            policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});

//-----------------------------------------------------//

// =========================
// 🌍 ENVIRONMENT BASED Middleware Pipeline
// =========================
var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors("AllowReact");

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapControllers();

app.UseStaticFiles();

app.Run();

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Domain.Entities;
using PmfApi.Filters;
using PmfApi.Application.Interfaces;
using PmfApi.Infrastructure.Persistence.Services;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using PmfApi.Api.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using PmfApi.Api.Hubs;
using PmfApi.Application.Abstractions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;


var builder = WebApplication.CreateBuilder(args);


builder.Services
.AddAuthentication("Pharmacy")
.AddScheme<AuthenticationSchemeOptions, PharmacyAuthHandler>("Pharmacy",null);

builder.Services.AddDbContext<PmfDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("PmfDatabase"))
.LogTo(Console.WriteLine, LogLevel.Information)
.EnableSensitiveDataLogging()
);

builder.Services.AddScoped<IPharmaciesService,PharamaciesSerivce>();
builder.Services.AddScoped<IMedicinesService,MedicinesService>();
builder.Services.AddScoped<ILocationService,LocationService>();
builder.Services.AddScoped<IPharmaciesScheduleService,PharmaciesScheduleService>();
builder.Services.AddScoped<IInventoryService,InventoryService>();
builder.Services.AddScoped<IInventoryHistoryService,InventoryHistoryService>();
builder.Services.AddScoped<IInventoryReviewService, InventoryReviewService>();
builder.Services.AddScoped<IUserFeedBackService,UserFeedBackService>();
builder.Services.AddScoped<IUserService,UserService>();
builder.Services.AddScoped<IAdminService,AdminService>();
builder.Services.AddScoped<IAccountRecoveryService,AccountRecoveryService>();
builder.Services.AddSingleton<IEmailSender,SmtpEmailSender>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IRoleService,RoleService>();
builder.Services.AddScoped<ISearchService,SearchService>();
builder.Services.AddScoped<IPharmacyStaffService,PharmacyStaffService>();
builder.Services.AddScoped<IPharmacyDocumentService,PharmacyDocumentService>();
builder.Services.AddScoped<IReviewService,ReviewService>();
builder.Services.AddScoped<IFavoriteService,FavoriteService>();
builder.Services.AddScoped<IPharmacyAdminService,PharmacyAdminService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService,AuthService>();
builder.Services.AddScoped<ITokenService,TokenService>();
builder.Services.AddScoped<IAuthorizationHandler, PharmacyOwnerHandler>();
builder.Services.AddScoped<IAuthorizationHandler, PharmacyDocumentReviewHandler>();
builder.Services.AddScoped<IFileStorage,LocalFileStorage>();
builder.Services.AddScoped<IPharmacyRegistrationService,PharmacyRegistrationService>();
builder.Services.AddScoped<IOrderService,OrderService>();
builder.Services.AddScoped<IOrderItemService,OrderItemService>();
builder.Services.AddScoped<IPrescriptionService,PrescriptionService>();
builder.Services.AddScoped<IAuthorizationHandler, PrescriptionVerificationHandler>();
builder.Services.AddScoped<IAlertService,AlertService>();
builder.Services.AddScoped<IDeviceTokenService,DeviceTokenService>();
builder.Services.AddScoped<IPmfHubClient, SignalRPmfHubClient>();
builder.Services.AddScoped<IPharmacyMembershipService, PharmacyMembershipService>();
builder.Services.AddScoped<IPharmacyDashboardService, PharmacyDashboardService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddSignalR();





builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogFilter>();
})
.ConfigureApiBehaviorOptions(options =>
{
    var defaultFactory = options.InvalidModelStateResponseFactory;
    options.InvalidModelStateResponseFactory = ctx =>
    {
        var errors = ctx.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .Select(e => $"{e.Key}: {string.Join("; ", e.Value!.Errors.Select(x => x.ErrorMessage))}");
        ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Validation")
            .LogWarning("400 validation failed on {Path} -> {Errors}", ctx.HttpContext.Request.Path, string.Join(" | ", errors));
        return defaultFactory(ctx);
    };
});

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // These must mirror what TokenService signs with. Adjust the config keys to yours.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = token;
                }
                return Task.CompletedTask;
            },

            // A valid signature is not enough: a suspended, deactivated or deleted account must stop
            // working at once, not when its token expires. The answer is cached for 15 seconds, and
            // the admin and self-delete code clear the cache entry so the change is immediate.
            OnTokenValidated = async ctx =>
            {
                if (!int.TryParse(ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                {
                    ctx.Fail("Invalid token.");
                    return;
                }

                var cache = ctx.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                if (!cache.TryGetValue(UserCacheKeys.CanUseApi(userId), out bool canUse))
                {
                    var db = ctx.HttpContext.RequestServices.GetRequiredService<PmfDbContext>();
                    canUse = await db.Users.AsNoTracking().AnyAsync(
                        u => u.Id == userId && u.IsActive && u.SuspendedAt == null && u.DeletedAt == null,
                        ctx.HttpContext.RequestAborted);
                    cache.Set(UserCacheKeys.CanUseApi(userId), canUse, TimeSpan.FromSeconds(15));
                }

                if (!canUse) ctx.Fail("This account is no longer active.");
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
   
    options.AddPolicy("PharmacyOwnerOrAdmin", policy =>
    policy.Requirements.Add(new PharmacyOwnerRequirement(
        allowPlatformAdmin: true,
        requiredElevatedStaffRole: true)));

   options.AddPolicy("PharmacyOwner", policy =>
    policy.Requirements.Add(new PharmacyOwnerRequirement()));
 
   
    options.AddPolicy("DocumentReview", policy =>
        policy.Requirements.Add(new PharmacyDocumentReviewRequirement()));

    options.AddPolicy("PrescriptionVerification", policy =>
        policy.Requirements.Add(new PrescriptionVerificationRequirement()));
});
 
builder.Services.AddRateLimiter(options =>
{
    
    options.AddTokenBucketLimiter("AuthLimiter", limiterOptions =>
    {
        limiterOptions.TokenLimit = 5;
        limiterOptions.TokensPerPeriod = 5;
        limiterOptions.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        limiterOptions.QueueLimit = 0;
        limiterOptions.AutoReplenishment = true;
    });
 
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                 
    app.MapScalarApiReference(); 

}

else
{
    app.UseExceptionHandler();
}

app.UseStatusCodePages();



app.UseMiddleware<RequestLoggingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowAngular");

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<PmfHub>("/hubs/pmf");



if (app.Environment.IsDevelopment()) { 
    using var scope = app.Services.CreateScope(); 
    var context = scope.ServiceProvider.GetRequiredService<PmfDbContext>(); 
    await DataSeeder.SeedAsync(context);
     }


app.MapGet("/api/error", () =>
{
    throw new PmfDatabaseException("Simulated database failure for ProblemDetails testing");
});
app.MapControllers();
app.Run();
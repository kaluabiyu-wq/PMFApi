

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
builder.Services.AddScoped<IUserFeedBackService,UserFeedBackService>();
builder.Services.AddScoped<IUserService,UserService>();
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


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogFilter>();
}

);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});


builder.Services.AddAuthorization(options =>
{
   
    options.AddPolicy("PharmacyOwnerOrAdmin", policy =>
        policy.Requirements.Add(new PharmacyOwnerRequirement(
            allowSystemAdmin: true,
            requiredElevatedStaffRole: true)));
 
    
    options.AddPolicy("PharmacyOwner", policy =>
        policy.Requirements.Add(new PharmacyOwnerRequirement()));
 
   
    options.AddPolicy("DocumentReview", policy =>
        policy.Requirements.Add(new PharmacyDocumentReviewRequirement()));
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
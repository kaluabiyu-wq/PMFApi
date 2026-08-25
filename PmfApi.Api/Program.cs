

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PmfApi.Infrastructure.Persistence;
using PmfApi.Domain.Entities;
using PmfApi.Filters;
using PmfApi.Application.Interfaces;
using PmfApi.Infrastructure.Persistence.Services;
using Scalar.AspNetCore;

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


builder.Services.AddAuthorization();

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
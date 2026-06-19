

using Microsoft.AspNetCore.Authentication;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
.AddAuthentication("Pharmacy")
.AddScheme<AuthenticationSchemeOptions, PharmacyAuthHandler>("Pharmacy",null);

builder.Services.AddSingleton<IPharmaciesService,PharamaciesSerivce>();
builder.Services.AddSingleton<IMedicinesService,MedicinesService>();
builder.Services.AddSingleton<ILocationService,LocationService>();



builder.Services.AddControllers();

var app = builder.Build();


// // Configure the HTTP request pipeline.
 app.MapGet("/api/pharmacies",()=> {
    return Results.Ok(new
    {
        Name = "Betezata",
        LicenceNumber = "P001234",
        Email = "Betezata@gmail.com",
        IsVerified = true,
        IsActive = true,
        ReliabilityScore = 56m,
        FreshnessThreshold = 5,
        LastinventoryUpdateAt = DateTime.UtcNow,
        RegisteredAt = DateTime.UtcNow
         
    });
    
 }).RequireAuthorization();

// app.UseHttpsRedirection();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

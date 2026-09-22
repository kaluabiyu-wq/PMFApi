

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;


var builder = WebApplication.CreateBuilder(args);


builder.Services
.AddAuthentication("Pharmacy")
.AddScheme<AuthenticationSchemeOptions, PharmacyAuthHandler>("Pharmacy",null);

builder.Services.AddSingleton<IPharmaciesService,PharamaciesSerivce>();
builder.Services.AddScoped<IMedicinesService,MedicinesService>();
builder.Services.AddScoped<ILocationService,LocationService>();
builder.Services.AddScoped<IPharmaciesScheduleService,PharmaciesScheduleService>();
builder.Services.AddScoped<IInventoryService,InventoryService>();
builder.Services.AddScoped<IInventoryHistoryService,InventoryHistoryService>();
builder.Services.AddScoped<IUserFeedBackService,UserFeedBackService>();
builder.Services.AddScoped<UserService,UserService>();


builder.Host.UseDefaultServiceProvider(Options =>
{
    Options.ValidateScopes = true;
    Options.ValidateOnBuild = true;
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();




// app.UseHttpsRedirection();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthentication();

app.UseAuthorization();
 app.MapPost("/api/pharmacies",async (IPharmaciesService svc) => 
 {
    
   var pharmacies =  await svc.CreateAsync("Betezata",
         "P10025",
        0920456532,
        "Betezata@gmail.com",
        true,
         true,
        56.5m,
        5);
        return Results.Ok(pharmacies);
         
    });
app.MapGet("/api/pharmacies/id",async (string id,IPharmaciesService svc) => 
 {
   var pharmacies = await svc.GetByIdAsync(id);
   return pharmacies is null ? Results.NotFound() : Results.Ok(pharmacies);

 });

 app.MapGet("/api/pharmacies", async (IPharmaciesService svc)=>
 {
    var all = await svc.GetAllAsync();
    return Results.Ok(all);
     
 });

 app.MapDelete("/api/pharmacies/id",async (string id,IPharmaciesService svc) => 
 {
   var deleted = await svc.DeleteAsync(id);
   return deleted ? Results.NotFound() : Results.NoContent();

 });

//app.MapControllers();

app.Run();

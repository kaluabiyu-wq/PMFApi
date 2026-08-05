

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);


builder.Services
.AddAuthentication("Pharmacy")
.AddScheme<AuthenticationSchemeOptions, PharmacyAuthHandler>("Pharmacy",null);

builder.Services.AddSingleton<IPharmaciesService,PharamaciesSerivce>();
builder.Services.AddSingleton<IMedicinesService,MedicinesService>();
builder.Services.AddSingleton<ILocationService,LocationService>();
builder.Services.AddSingleton<IPharmaciesScheduleService,PharmaciesScheduleService>();
builder.Services.AddSingleton<IInventoryService,InventoryService>();
builder.Services.AddSingleton<IInventoryHistoryService,InventoryHistoryService>();
builder.Services.AddSingleton<IUserFeedBackService,UserFeedBackService>();
builder.Services.AddSingleton<UserService,UserService>();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddControllers();

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


app.MapControllers();
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



app.MapGet("/api/error", () =>
{
    throw new PmfDatabaseException("Simulated database failure for ProblemDetails testing");
});
app.Run();
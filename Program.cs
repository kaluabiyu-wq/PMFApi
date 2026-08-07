

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PmfApi.Data;
using PmfApi.Entities;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services
.AddAuthentication("Pharmacy")
.AddScheme<AuthenticationSchemeOptions, PharmacyAuthHandler>("Pharmacy",null);

builder.Services.AddDbContext<PmfDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("PmfDatabase")
));

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


using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PmfDbContext>();

    context.Database.Migrate();
    if (!context.Roles.Any())
    {
        var roles = new List<Role>
        {
            new() { Name = "Patient"},
            new() { Name = "PhramacyStaff"},
            new() { Name = "PharmacyAdmin"},
            new() { Name = "SystemAdmin"},
        };
        context.Roles.AddRange(roles);
    

     var Locations = new List<Location>
     {
         new() {Label ="Bole" , Latitude = 8.9954m, Longitude = 39.9995m, Subcity = "Bole",Woreda ="w-01" },
         new() {Label ="Piasa" , Latitude = 7.9954m, Longitude = 29.9995m, Subcity = "Piasa",Woreda ="w-05" },
         new() {Label ="Autobistera" , Latitude = 5.9954m, Longitude = 29.9995m, Subcity = "Addis Ketema",Woreda ="w-05" },
         new() {Label ="Kazanchis" , Latitude = 7.9954m, Longitude = 29.9995m, Subcity = "Kirckos",Woreda ="w-05" },
     };
     context.Locations.AddRange(Locations);
     context.SaveChanges();

     var user = new List<User>
     {
         new() { FullName = "Beza Getachew", Email = "Betezata@gmail.com", Password = "Betezata#123A", RoleId = roles[1].Id, LocationId = Locations[1].Id,IsActive = false},
         new() { FullName = "Selam Ayele", Email = "Selam@gmail.com", Password = "Selam#123A", RoleId = roles[2].Id, LocationId = Locations[3].Id, IsActive = true},
         new() { FullName = "Daniel Ayele", Email = "Daniel@gmail.com", Password = "Daniel#123A", RoleId = roles[0].Id, LocationId = Locations[2].Id,IsActive = true},
         new() { FullName = "Kalu Abiyu", Email = "Kalu@gmail.com", Password = "Kalu#123A", RoleId = roles[3].Id, LocationId = Locations[2].Id, IsActive = true},

     };
     context.Users.AddRange(user);

     var pharmacies = new List<Pharmacy>
     {
      new() { Name = "Betezata Pharmacy", LicenceNumber = "LIC-0001", IsVerified = true, ReliablityScore = 88m, LocationId = Locations[0].Id },
      new() { Name = "Amanuel Pharmacy", LicenceNumber = "LIC-0002", IsVerified = true, ReliablityScore = 74m, LocationId = Locations[1].Id }, 
      new() { Name = "Kazanchis Community Pharmacy", LicenceNumber = "LIC-0003", IsVerified = false, ReliablityScore = 55m, LocationId = Locations[2].Id },
      
      }; 
      context.Pharmacies.AddRange(pharmacies); 
      
      
      var medicines = new List<Medicine>
    {
     new() { GenericName = "Paracetamol", BrandName = "Panadol", Category = "Analgesic", RequeiresPrescription = false },
     new() { GenericName = "Amoxicillin", BrandName = "Amoxil", Category = "Antibiotic", RequeiresPrescription = true },
     new() { GenericName = "Metformin", BrandName ="Amoxcil" ,Category = "Antidiabetic", RequeiresPrescription = true }, 
      
      }; 
      context.Medicines.AddRange(medicines);
        context.SaveChanges();

         
     
    
var inventory = new List<Inventory>
 { 
  new() { PharmacyId = pharmacies[0].Id, MedicineId = medicines[0].Id, Price = 45m, UserId = user[0].Id },
  new() { PharmacyId = pharmacies[0].Id, MedicineId = medicines[1].Id, Price = 65m, UserId = user[1].Id },
  new() { PharmacyId = pharmacies[1].Id, MedicineId = medicines[0].Id, Price = 125m, UserId = user[2].Id },
  new() { PharmacyId = pharmacies[1].Id, MedicineId = medicines[1].Id, Price = 135m, UserId = user[1].Id },
 
 };

 context.Inventories.AddRange(inventory);
  context.SaveChanges(); 
  
  
   }

}



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
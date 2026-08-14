

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PmfApi.Data;
using PmfApi.Entities;
using PmfApi.Interface;
using PmfApi.Service;
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
         new() {Label ="Piasa" , Latitude = 5.9954m, Longitude = 29.9995m, Subcity = "Piasa",Woreda ="w-05" },
         new() {Label ="Autobistera" , Latitude = 9.0357m, Longitude = 38.7460m, Subcity = "Addis Ketema",Woreda ="w-05" },
         new() {Label ="Kazanchis" , Latitude = 9.0180m, Longitude = 38.7660m, Subcity = "Kirckos",Woreda ="w-01" },
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
    new() { Name = "Babi Pharmacy", LicenceNumber = "LIC-0004", IsVerified = true, ReliablityScore = 52m, LocationId = Locations[3].Id },
        
      }; 
      context.Pharmacies.AddRange(pharmacies); 
      
      
      var medicines = new List<Medicine>
    {
     new() { GenericName = "Paracetamol", BrandName = "Panadol", Category = "Analgesic", RequeiresPrescription = false },
     new() { GenericName = "Amoxicillin", BrandName = "Amoxil", Category = "Antibiotic", RequeiresPrescription = true },
     new() { GenericName = "Metformin", BrandName ="Glucophage" ,Category = "Antidiabetic", RequeiresPrescription = true }, 
       
      }; 
      context.Medicines.AddRange(medicines);
        context.SaveChanges();

         
     
var inventory = new List<Inventory>
 { 
  new() { PharmacyId = pharmacies[0].Id, MedicineId = medicines[0].Id, Price = 245m, UserId = user[0].Id,Status = "Fresh" },
  new() { PharmacyId = pharmacies[0].Id, MedicineId = medicines[1].Id, Price = 65m, UserId = user[1].Id ,Status ="stale"},
  new() { PharmacyId = pharmacies[1].Id, MedicineId = medicines[0].Id, Price = 225m, UserId = user[2].Id,Status = "stale" },
  new() { PharmacyId = pharmacies[1].Id, MedicineId = medicines[1].Id, Price = 55m, UserId = user[1].Id , Status = "Fresh" },
  new() { PharmacyId = pharmacies[2].Id, MedicineId = medicines[0].Id, Price = 235m, UserId = user[2].Id, Status = "Fresh" },
  new() { PharmacyId = pharmacies[2].Id, MedicineId = medicines[1].Id, Price = 75m, UserId = user[1].Id , Status = "Fresh"},
  new() { PharmacyId = pharmacies[3].Id, MedicineId = medicines[0].Id, Price = 265m, UserId = user[2].Id, Status = "Fresh" },
  new() { PharmacyId = pharmacies[3].Id, MedicineId = medicines[1].Id, Price = 85m, UserId = user[1].Id , Status = "Fresh"},
 
 }; 

 context.Inventories.AddRange(inventory);
  context.SaveChanges(); 
  
  
   }

}

app.MapGet("/api/error", () =>
{
    throw new PmfDatabaseException("Simulated database failure for ProblemDetails testing");
});
app.Run();
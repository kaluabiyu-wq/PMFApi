using Microsoft.EntityFrameworkCore;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence;

public static class DataSeeder
{
    private static readonly (string Name, string Description)[] Roles =
    [
        ("Patient", "Searches medicine availability and views pharmacy details"),
        ("Pharmacy", "Owns and manages a registered pharmacy account"),
        ("PharmacyStaff", "Manages inventory on behalf of a pharmacy"),
        ("Admin", "System-wide administrator"),
    ];

    private static readonly (string Label, string Subcity, string Woreda,
       decimal Lat, decimal Lng)[] Locations =
    [
       ("Bole Branch Area", "Bole", "03", 8.9806m, 38.7578m),
       ("Kirkos Branch Area", "Kirkos", "07", 9.0107m, 38.7613m),
       ("Yeka Branch Area", "Yeka", "05", 9.0303m, 38.8091m),
       ("Arada Branch Area", "Arada", "10", 9.0350m, 38.7469m),
       ("Addis Ketema Branch Area", "Addis Ketema", "08", 9.0339m, 38.7370m),
       ("Lideta Branch Area", "Lideta", "04", 9.0154m, 38.7333m),
       ("Nifas Silk-Lafto Branch Area", "Nifas Silk-Lafto", "06", 8.9636m, 38.7378m),
       ("Kolfe Keranio Branch Area", "Kolfe Keranio", "09", 9.0192m, 38.6989m),
       ("Gulele Branch Area", "Gulele", "02", 9.0500m, 38.7333m),
       ("Akaky Kaliti Branch Area", "Akaky Kaliti", "11", 8.8833m, 38.7500m),
    ];

    private static readonly (string GenericName, string BrandName,
     string Category, string DosageForm, string Strength,
     bool RequiresPrescription)[] Medicines =
    [
        ("Paracetamol", "Panadol", "Analgesic", "Tablet", "500mg", false),
        ("Amoxicillin", "Amoxil", "Antibiotic", "Capsule", "500mg", true),
        ("Ibuprofen", "Brufen", "Analgesic", "Tablet", "400mg", false),
        ("Omeprazole", "Losec", "Antacid", "Capsule", "20mg", false),
        ("Metformin", "Glucophage", "Antidiabetic", "Tablet", "500mg", true),
        ("Atorvastatin", "Lipitor", "Cardiovascular", "Tablet", "20mg", true),
        ("Amlodipine", "Norvasc", "Cardiovascular", "Tablet", "5mg", true),
        ("Ciprofloxacin", "Ciproxin", "Antibiotic", "Tablet", "500mg", true),
        ("Salbutamol", "Ventolin", "Respiratory", "Inhaler", "100mcg", true),
        ("Ascorbic Acid", "Vitamin C", "Supplement", "Tablet", "1000mg", false),
        ("Artemether/Lumefantrine", "Coartem", "Antimalarial", "Tablet", "20/120mg", true),
        ("Diclofenac", "Voltaren", "Analgesic", "Tablet", "50mg", false),
        ("Metronidazole", "Flagyl", "Antiparasitic", "Tablet", "400mg", true),
        ("Doxycycline", "Vibramycin", "Antibiotic", "Capsule", "100mg", true),
        ("Azithromycin", "Zithromax", "Antibiotic", "Tablet", "500mg", true),
        ("Losartan", "Cozaar", "Cardiovascular", "Tablet", "50mg", true),
        ("Hydrochlorothiazide", "Microzide", "Cardiovascular", "Tablet", "25mg", true),
        ("Cetirizine", "Zyrtec", "Antihistamine", "Tablet", "10mg", false),
        ("Loratadine", "Claritin", "Antihistamine", "Tablet", "10mg", false),
        ("Famotidine", "Pepcid", "Antacid", "Tablet", "40mg", false),
        ("Multivitamin", "Supradyn", "Supplement", "Tablet", "N/A", false),
        ("Ferrous Sulfate", "Feosol", "Supplement", "Tablet", "200mg", false),
        ("Folic Acid", "Folvite", "Supplement", "Tablet", "5mg", false),
        ("Vitamin B Complex", "Neurobion", "Supplement", "Tablet", "N/A", false),
        ("Insulin (Human)", "Actrapid", "Antidiabetic", "Injection", "100IU/ml", true),
        ("Glibenclamide", "Daonil", "Antidiabetic", "Tablet", "5mg", true),
        ("Prednisolone", "Deltacortril", "Corticosteroid", "Tablet", "5mg", true),
        ("Tramadol", "Tramal", "Analgesic", "Capsule", "50mg", true),
        ("Oral Rehydration Salts", "ORS", "Electrolyte", "Sachet", "N/A", false),
        ("Albendazole", "Zentel", "Antiparasitic", "Tablet", "400mg", false),
    ];

    private static readonly (string Name, string LicenceNumber,
     int PhoneNumber, string Email, bool IsVerified,
     decimal ReliablityScore, int FreshnessThreshold, int LocationIndex)
     [] Pharmacies =
    [
       ("Kenema Pharmacy", "LIC-10001", 911100001, "info@kenemapharmacy.et", true, 4.6m, 30, 0),
       ("Tikur Anbessa Pharmacy", "LIC-10002", 911100002, "info@tikuranbessa.et", true, 4.8m, 30, 1),
       ("Zewditu Pharmacy", "LIC-10003", 911100003, "info@zewditupharmacy.et", true, 4.2m, 21, 2),
       ("Bole Fenta Pharmacy", "LIC-10004", 911100004, "info@bolefenta.et", true, 4.4m, 30, 0),
       ("Addis Cure Pharmacy", "LIC-10005", 911100005, "info@addiscure.et", false, 3.9m, 14, 3),
       ("Sunshine Pharmacy", "LIC-10006", 911100006, "info@sunshinepharmacy.et", true, 4.1m, 30, 1),
       ("Family Guard Pharmacy", "LIC-10007", 911100007, "info@familyguard.et", true, 4.7m, 30, 2),
       ("Nile Health Pharmacy", "LIC-10008", 911100008, "info@nilehealth.et", false, 3.5m, 21, 3),
       ("Grace Pharmacy", "LIC-10009", 911100009, "info@gracepharmacy.et", true, 4.3m, 30, 0),
       ("Unity Pharmacy", "LIC-10010", 911100010, "info@unitypharmacy.et", true, 4.0m, 30, 1),
       ("Adey Pharmacy", "LIC-10011", 911100011, "info@adeypharmacy.et", true, 4.5m, 30, 4),
       ("Meskel Square Pharmacy", "LIC-10012", 911100012, "info@meskelsquarepharmacy.et", true, 4.3m, 21, 5),
       ("Piassa Central Pharmacy", "LIC-10013", 911100013, "info@piassacentral.et", false, 3.8m, 14, 6),
       ("Merkato Pharmacy", "LIC-10014", 911100014, "info@merkatopharmacy.et", true, 4.1m, 30, 7),
       ("CMC Health Pharmacy", "LIC-10015", 911100015, "info@cmchealth.et", true, 4.6m, 30, 8),
       ("Saris Pharmacy", "LIC-10016", 911100016, "info@sarispharmacy.et", true, 4.2m, 30, 9),
       ("Gerji Pharmacy", "LIC-10017", 911100017, "info@gerjipharmacy.et", false, 3.7m, 21, 0),
       ("Megenagna Pharmacy", "LIC-10018", 911100018, "info@megenagnapharmacy.et", true, 4.4m, 30, 1),
       ("Summit Pharmacy", "LIC-10019", 911100019, "info@summitpharmacy.et", true, 4.5m, 30, 2),
       ("St. Gabriel Pharmacy", "LIC-10020", 911100020, "info@stgabrielpharmacy.et", true, 4.9m, 30, 3),
    ];

    private static readonly (string FullName, string Email,
     string RoleName, int LocationIndex)[] Users =
    [
        ("Selamawit Bekele", "selamawit.bekele@example.com", "Patient", 0),
        ("Dawit Alemu", "dawit.alemu@example.com", "Patient", 1),
        ("Hanna Girma", "hanna.girma@example.com", "Patient", 2),
        ("Yonas Tesfaye", "yonas.tesfaye@example.com", "Patient", 3),
        ("Marta Solomon", "marta.solomon@example.com", "Patient", 0),
        ("Abel Kebede", "abel.kebede@kenemapharmacy.et", "PharmacyStaff", 0),
        ("Meklit Fikru", "meklit.fikru@tikuranbessa.et", "PharmacyStaff", 1),
        ("Nathnael Wolde", "nathnael.wolde@zewditupharmacy.et", "PharmacyStaff", 2),
        ("Ruth Assefa", "ruth.assefa@bolefenta.et", "PharmacyStaff", 0),
        ("Kalkidan Mulu", "kalkidan.mulu@addiscure.et", "PharmacyStaff", 3),
        ("Biniam Tadesse", "biniam.tadesse@kenemapharmacy.et", "Pharmacy", 0),
        ("System Administrator", "admin@pmf.et", "Admin", 1),
    ];

      private static readonly decimal[] BasePrices =
    [
        45m, 120m, 60m,  95m,  70m, 
        210m, 180m, 130m, 250m, 50m,   
        150m,  55m, 65m,  140m, 160m,  
        110m, 90m, 60m,  60m,  75m, 
        85m,  70m,  40m, 80m,   320m, 
        95m, 100m, 90m, 30m, 55m,  
    ];

    public static async Task SeedAsync(PmfDbContext context,
     CancellationToken ct = default)
    {
        await context.Database.MigrateAsync(ct);

        if (await context.Roles.AnyAsync(ct))
        {
            return;
        }

        foreach (var (name, description) in Roles)
        {
            context.Roles.Add(new Role { Name = name, Description = description });
        }
        await context.SaveChangesAsync(ct);

        foreach (var (label, subcity, woreda, lat, lng) in Locations)
        {
            context.Locations.Add(new Location
            {
                Label = label,
                Subcity = subcity,
                Woreda = woreda,
                Coordinate = new Coordinate { Latitude = lat, Longitude = lng }
            });
        }
        await context.SaveChangesAsync(ct);

        foreach (var (genericName, brandName, category,
         dosageForm, strength, requiresPrescription) in Medicines)
        {
            context.Medicines.Add(new Medicine
            {
                GenericName = genericName,
                BrandName = brandName,
                Category = category,
                DosageForm = dosageForm,
                Strength = strength,
                RequeiresPrescription = requiresPrescription,
                IsActive = true
            });
        }
        await context.SaveChangesAsync(ct);

        var locations = await context.Locations.OrderBy(l => l.Id).ToListAsync(ct);

        foreach (var (name, licenceNumber, phoneNumber, email,
         isVerified, reliablityScore, freshnessThreshold, locationIndex)
          in Pharmacies)
        {
            context.Pharmacies.Add(new Pharmacy
            {
                Name = name,
                LicenceNumber = licenceNumber,
                PhoneNumber = phoneNumber,
                Email = email,
                IsVerified = isVerified,
                ReliablityScore = reliablityScore,
                FreshnessThreshold = freshnessThreshold,
                LastInventoryUpdateAt = DateTime.UtcNow.AddDays(-1),
                LocationId = locations[locationIndex].Id,
                IsActive = true
            });
        }
        await context.SaveChangesAsync(ct);

        var pharmacies = await context.Pharmacies.OrderBy(p => p.Id).ToListAsync(ct);

        foreach (var pharmacy in pharmacies)
        {
            for (var dow = 0; dow < 7; dow++)
            {
                context.PharmaciesSchedules.Add(new PharmaciesSchedule
                {
                    PharmacyId = pharmacy.Id,
                    DayOfWeek = dow,
                    OpenTime = DateTime.UtcNow.Date.AddHours(8),
                    ClosedTime = DateTime.UtcNow.Date.AddHours(20),
                    ISClosed = false
                });
            }
        }
        await context.SaveChangesAsync(ct);

        var roles = await context.Roles.ToListAsync(ct);

        foreach (var (fullName, email, roleName, locationIndex) in Users)
        {
            context.Users.Add(new User
            {
                FullName = fullName,
                Email = email,
                Password = "Seed$Passw0rd!",
                RoleId = roles.First(r => r.Name == roleName).Id,
                LocationId = locations[locationIndex].Id
            });
        }
        await context.SaveChangesAsync(ct);

        var medicines = await context.Medicines.OrderBy(m => m.Id).ToListAsync(ct);
        var pharmacyStaff = await context.Users
            .Where(u => u.Role.Name == "PharmacyStaff")
            .OrderBy(u => u.Id)
            .ToListAsync(ct);

        var random = new Random(42);

        for (var p = 0; p < pharmacies.Count; p++)
        {
            var pharmacy = pharmacies[p];
            var staff = pharmacyStaff[p % pharmacyStaff.Count];

            for (var m = 0; m < medicines.Count; m++)
            {
                var daysAgo = random.Next(0, pharmacy.FreshnessThreshold + 15);
                var lastUpdatedAt = DateTime.UtcNow.AddDays(-daysAgo);
                var status = daysAgo <= pharmacy.FreshnessThreshold ? "Fresh" : "Stale";

                context.Inventories.Add(new Inventory
                {
                    PharmacyId = pharmacy.Id,
                    MedicineId = medicines[m].Id,
                    UpdatebyUserId = staff.Id,
                    Price = BasePrices[m] + (p * 5m),
                    Status = status,
                    LastUpdatedAt = lastUpdatedAt
                });
            }
        }
        await context.SaveChangesAsync(ct);
    }
}
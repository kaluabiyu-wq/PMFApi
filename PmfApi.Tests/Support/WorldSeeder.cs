using PmfApi.Domain.Entities;
using PmfApi.Infrastructure.Persistence;

namespace PmfApi.Tests.Support;

public static class WorldSeeder
{
    
    private const string RolePatient = "Patient";
    private const string RolePharmacy = "Pharmacy";
    private const string RolePharmacyAdmin = "PharmacyAdmin";
    private const string RoleSysAdmin = "SysAdmin";

    public static async Task<TestWorld> SeedAsync(PmfDbContext db)
    {
        
        var roles = new[] { RolePatient, RolePharmacy, RolePharmacyAdmin, RoleSysAdmin }
            .Select(n => new Role { Name = n })
            .ToList();
        db.Roles.AddRange(roles);

        var location = new Location
        {
            Label = "Test Area",
            Subcity = "Bole",
            Woreda = "03",
            Coordinate = new Coordinate { Latitude = 9.0m, Longitude = 38.7m },
        };
        db.Locations.Add(location);
        await db.SaveChangesAsync();

        
        int RoleId(string name) => roles.Single(r => r.Name == name).Id;

        User NewUser(string handle, string role) => new()
        {
            FullName = handle,
            Email = $"{handle}@pmf.test",
            Password = "not-a-real-hash",
            RoleId = RoleId(role),
            LocationId = location.Id,
        };

        var patientA = NewUser("patient-a", RolePatient);
        var patientB = NewUser("patient-b", RolePatient);
        var sysAdmin = NewUser("sys-admin", RoleSysAdmin);
        var pharmacyAdmin = NewUser("pharmacy-admin", RolePharmacyAdmin);
        var ownerA = NewUser("owner-a", RolePharmacy);
        var ownerB = NewUser("owner-b", RolePharmacy);
        db.Users.AddRange(patientA, patientB, sysAdmin, pharmacyAdmin, ownerA, ownerB);

        
        Pharmacy NewPharmacy(string name, string licence, int phone) => new()
        {
            Name = name,
            LicenceNumber = licence,
            PhoneNumber = phone,
            Email = $"{licence.ToLowerInvariant()}@pharmacy.test",
            IsVerified = true,
            IsActive = true,
            ReliablityScore = 4m,
            FreshnessThreshold = 24,
            LastInventoryUpdateAt = DateTime.UtcNow,
            RegisteredAt = DateTime.UtcNow,
            LocationId = location.Id,
        };

        var pharmacyA = NewPharmacy("Pharmacy A", "LIC-A", 911000001);
        var pharmacyB = NewPharmacy("Pharmacy B", "LIC-B", 911000002);
        var pharmacyC = NewPharmacy("Pharmacy C", "LIC-C", 911000003);
        db.Pharmacies.AddRange(pharmacyA, pharmacyB, pharmacyC);

        var rxMedicine = new Medicine { GenericName = "Amoxicillin", BrandName = "Amoxil", RequeiresPrescription = true };
        var otcMedicine = new Medicine { GenericName = "Paracetamol", BrandName = "Panadol", RequeiresPrescription = false };
        db.Medicines.AddRange(rxMedicine, otcMedicine);
        await db.SaveChangesAsync();


        db.PharmacyStaff.AddRange(
            new PharmacyStaff { UserId = ownerA.Id, PharmacyId = pharmacyA.Id, Position = "Pharmacist", IsActive = true },
            new PharmacyStaff { UserId = ownerB.Id, PharmacyId = pharmacyB.Id, Position = "Pharmacist", IsActive = true });

        Inventory NewInventory(Medicine medicine, Pharmacy pharmacy, decimal price) => new()
        {
            MedicineId = medicine.Id,
            PharmacyId = pharmacy.Id,
            Price = price,
            UpdatebyUserId = ownerA.Id,
            ApprovalStatus = InventoryApprovalStatus.Approved,
        };

        var rxA = NewInventory(rxMedicine, pharmacyA, 120m);
        var otcA = NewInventory(otcMedicine, pharmacyA, 15m);
        var rxB = NewInventory(rxMedicine, pharmacyB, 130m);
        db.Inventories.AddRange(rxA, otcA, rxB);
        await db.SaveChangesAsync();

        TestUser Of(User u, string role) => new(u.Id, u.Email, role);

        return new TestWorld(
            PatientA: Of(patientA, RolePatient),
            PatientB: Of(patientB, RolePatient),
            SysAdmin: Of(sysAdmin, RoleSysAdmin),
            PharmacyAdmin: Of(pharmacyAdmin, RolePharmacyAdmin),
            OwnerA: Of(ownerA, RolePharmacy),
            OwnerB: Of(ownerB, RolePharmacy),
            PharmacyAId: pharmacyA.Id,
            PharmacyBId: pharmacyB.Id,
            PharmacyCId: pharmacyC.Id,
            RxInventoryA: rxA.Id,
            OtcInventoryA: otcA.Id,
            RxInventoryB: rxB.Id);
    }
}

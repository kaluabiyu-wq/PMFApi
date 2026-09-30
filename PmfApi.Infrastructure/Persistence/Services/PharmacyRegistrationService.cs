using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using PmfApi.Application.Dtos;
using PmfApi.Application.Exceptions;
using PmfApi.Application.Interfaces;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence.Services;

public class PharmacyRegistrationService(
    PmfDbContext context,
    IPasswordHasher<User> hasher,
    IFileStorage storage,
    ILogger<PharmacyRegistrationService> logger) : IPharmacyRegistrationService
{
    private const string PharmacyRoleName = "Pharmacy"; // seeded in DataSeeder

    public async Task<RegisterPharmacyResponse> RegisterAsync(RegisterPharmacyRequest req, CancellationToken ct)
    {
        var email = req.Email.Trim();

        if (await context.Users.AnyAsync(u => u.Email == email, ct))
         throw new DuplicateEmailException();
        if (await context.Pharmacies.AnyAsync(p => p.LicenceNumber == req.LicenseNumber, ct))
            throw new DuplicateLicenseException();
        if (!await context.Locations.AnyAsync(l => l.Id == req.LocationId, ct))
            throw new LocationNotFoundException();

        var roleId = await context.Roles.Where(r => r.Name == PharmacyRoleName)
            .Select(r => r.Id).SingleAsync(ct);

        var savedFiles = new List<string>();
        try
        {
            var uploads = new (DocumentType Type, IFormFile File)[]
            {
                (DocumentType.License, req.License),
                (DocumentType.BusinessRegistration, req.BusinessRegistration),
                (DocumentType.PharmacistCredential, req.PharmacistCredential),
            };

            var documents = new List<PharmacyDocument>();
            foreach (var (type, file) in uploads)
            {
                var path = await storage.SaveAsync(file, "pharmacy-documents", ct);
                savedFiles.Add(path);
                documents.Add(new PharmacyDocument
                {
                    DocumentType = type,
                    FileUrl = path,
                    ReviewStatus = ReviewStatus.Pending,
                });
            }

            var user = new User
            {
                FullName = req.FullName,
                Email = email,
                Password = string.Empty,
                RoleId = roleId,
                LocationId = req.LocationId,
            };
            user.Password = hasher.HashPassword(user, req.Password);

            var pharmacy = new Pharmacy
            {
                Name = req.PharmacyName,
                LicenceNumber = req.LicenseNumber,
                PhoneNumber = req.PhoneNumber,
                Email = email,
                LocationId = req.LocationId,
                IsVerified = false,
                FreshnessThreshold = 24,
                LastInventoryUpdateAt = DateTime.UtcNow,
                Documents = documents,
            };
            pharmacy.PharmacyStaff.Add(new PharmacyStaff { User = user, Position = "Owner" });

            // One SaveChanges = one implicit transaction: user, pharmacy, staff link and
            // documents are all written together or not at all.
            context.Pharmacies.Add(pharmacy);
            await context.SaveChangesAsync(ct);

            logger.LogInformation("Registered pharmacy {PharmacyId} with owner {UserId}", pharmacy.Id, user.Id);
            return new RegisterPharmacyResponse(pharmacy.Id, user.Id);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" } pg)
        {
            // Lost a race against another registration; the unique indexes are the source of truth.
            Cleanup(savedFiles);
            throw pg.ConstraintName?.Contains("Email", StringComparison.OrdinalIgnoreCase) == true
                ? new DuplicateEmailException()
                : new DuplicateLicenseException();
        }
        catch
        {
            Cleanup(savedFiles); // files are not part of the DB transaction
            throw;
        }
    }

    private void Cleanup(IEnumerable<string> paths)
    {
        foreach (var p in paths)
            try { 
                storage.Delete(p); 
                } 
            catch (Exception e) 
            {
                 logger.LogWarning(e, "Could not delete {Path}", p);
                  }
    }
}
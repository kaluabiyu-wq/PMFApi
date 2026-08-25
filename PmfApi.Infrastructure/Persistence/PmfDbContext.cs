

using Microsoft.EntityFrameworkCore;
using PmfApi.Domain.Entities;

namespace PmfApi.Infrastructure.Persistence;

public class PmfDbContext (DbContextOptions<PmfDbContext> options) :
 DbContext(options)
{
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Pharmacy> Pharmacies => Set<Pharmacy>();

    public DbSet<Medicine> Medicines => Set<Medicine>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<PharmaciesSchedule> PharmaciesSchedules => Set<PharmaciesSchedule>();

    public DbSet<InventoryHistory> InventoryHistories => Set<InventoryHistory>();

    public DbSet<UserFeedback> UserFeedbacks => Set<UserFeedback>();

    public DbSet<Search> Searches => Set<Search>();

   

     protected override void OnModelCreating(ModelBuilder b)
    {
      b.ApplyConfigurationsFromAssembly(typeof(PmfDbContext).Assembly);
    }
 

}
using Microsoft.EntityFrameworkCore;
using RekeszAppBackend.Domain;

namespace RekeszAppBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Partner> Partnerek => Set<Partner>();
    public DbSet<Vevo> Vevek => Set<Vevo>();
    public DbSet<Zoldseg> Zoldsegek => Set<Zoldseg>();
    public DbSet<RekeszTipus> RekeszTipusok => Set<RekeszTipus>();
    public DbSet<FelvasarlasTetel> FelvasarlasTetelek => Set<FelvasarlasTetel>();
    public DbSet<EladasTetel> EladasTetelek => Set<EladasTetel>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(x => x.Felhasznalonev).IsUnique();
        b.Entity<Zoldseg>().HasIndex(x => x.Nev).IsUnique();
        b.Entity<RekeszTipus>().HasIndex(x => x.Nev).IsUnique();

        b.Entity<Zoldseg>()
            .HasOne(x => x.AlapertelmezettRekeszTipus).WithMany().HasForeignKey(x => x.AlapertelmezettRekeszTipusId)
            .OnDelete(DeleteBehavior.Restrict);

        b.Entity<FelvasarlasTetel>().HasIndex(x => x.Datum);
        b.Entity<FelvasarlasTetel>()
            .HasOne(x => x.Partner).WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<FelvasarlasTetel>()
            .HasOne(x => x.Zoldseg).WithMany().HasForeignKey(x => x.ZoldsegId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<FelvasarlasTetel>()
            .HasOne(x => x.RekeszTipus).WithMany().HasForeignKey(x => x.RekeszTipusId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<FelvasarlasTetel>()
            .Property(x => x.Egysegar).HasPrecision(10, 2);

        b.Entity<EladasTetel>().HasIndex(x => x.Datum);
        b.Entity<EladasTetel>()
            .HasOne(x => x.Vevo).WithMany().HasForeignKey(x => x.VevoId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EladasTetel>()
            .HasOne(x => x.Zoldseg).WithMany().HasForeignKey(x => x.ZoldsegId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EladasTetel>()
            .HasOne(x => x.RekeszTipus).WithMany().HasForeignKey(x => x.RekeszTipusId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<EladasTetel>()
            .Property(x => x.Egysegar).HasPrecision(10, 2);
    }
}

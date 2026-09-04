using Microsoft.EntityFrameworkCore;
using RekeszAppBackend.Domain;

namespace RekeszAppBackend.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync(x => x.Felhasznalonev == "admin"))
            db.Users.Add(new User
            {
                Felhasznalonev = "admin",
                JelszoHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                Role = FelhasznaloSzerepkor.Admin
            });

        foreach (var nev in new[] { "M10", "M30" })
            if (!await db.RekeszTipusok.AnyAsync(x => x.Nev == nev))
                db.RekeszTipusok.Add(new RekeszTipus { Nev = nev });

        await db.SaveChangesAsync();
    }
}

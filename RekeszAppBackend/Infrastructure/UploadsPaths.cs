namespace RekeszAppBackend.Infrastructure;

// Feltöltött képek (zöldség-kategória fotók) tárolási helyének feloldása.
// Alapértelmezetten wwwroot/uploads - ez viszont sok megosztott tárhelyen (pl. runasp.net)
// minden újratelepítéskor kiürül, mert csak a wwwroot tartalma kerül szinkronizálásra.
// Az appsettings.json "Uploads:Path" kulcsával egy, a deploy által NEM érintett, tartós
// mappára állítható (pl. a wwwroot melletti "private" mappa, ha a tárhely ilyet kínál és
// azt a deploy folyamat nem írja felül - ezt érdemes előbb kézzel leellenőrizni).
// A mentett fájlok URL-je (/uploads/...) ettől függetlenül nem változik, mert a Program.cs
// egy külön static-files middleware-t köt a request-path elé, ami erre a mappára mutat.
public static class UploadsPaths
{
    public static string Resolve(IWebHostEnvironment env, IConfiguration config)
    {
        var configured = config["Uploads:Path"];
        string path;
        if (string.IsNullOrWhiteSpace(configured))
        {
            path = Path.Combine(env.ContentRootPath, "wwwroot", "uploads");
        }
        else if (Path.IsPathRooted(configured))
        {
            path = configured;
        }
        else
        {
            // Relatív útvonal a ContentRootPath-hoz képest, pl. "../private/uploads".
            path = Path.GetFullPath(Path.Combine(env.ContentRootPath, configured));
        }
        Directory.CreateDirectory(path);
        return path;
    }

    // Egy korábban elmentett kép fizikai törlése a lemezről, a benne tárolt "/uploads/xxx.jpg"
    // relatív URL alapján. Csendben nem csinál semmit, ha a fájl már nincs meg vagy az url üres -
    // nem szabad hogy egy hiányzó régi fájl megakassza az új kép mentését.
    public static void DeleteIfExists(IWebHostEnvironment env, IConfiguration config, string? kepUrl)
    {
        if (string.IsNullOrWhiteSpace(kepUrl) || !kepUrl.StartsWith("/uploads/")) return;
        try
        {
            var fileName = kepUrl["/uploads/".Length..];
            var filePath = Path.Combine(Resolve(env, config), fileName);
            if (File.Exists(filePath)) File.Delete(filePath);
        }
        catch { /* a régi fájl törlése soha ne akassza meg az új kép mentését */ }
    }
}

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace RekeszAppBackend.Infrastructure;

// Feltöltött zöldség-kategória fotók átméretezése és tömörítése, mielőtt lemezre kerülnek.
// A csempéken a kép soha nem jelenik meg kb. 500-600px szélességnél nagyobban, úgyhogy egy
// telefonnal készült, akár 4000px széles fotót nem érdemes eredeti méretben tárolni -
// feleslegesen sok helyet foglalna és lassítaná a betöltést, különösen mobil neten.
public static class ImageProcessing
{
    private const int MaxDimension = 1000; // px, hosszabbik oldal - bőven elég egy retina csempéhez is
    private const int JpegQuality = 80;

    // Beolvassa a feltöltött fájlt, szükség esetén arányosan lekicsinyíti, majd JPEG-ként menti
    // a megadott elérési útra. A visszaadott fájlnév-kiterjesztés mindig ".jpg" (a bemeneti
    // formátumtól függetlenül újrakódolunk, hogy a méretkorlátozás garantáltan érvényesüljön).
    public static async Task SaveResizedAsync(Stream input, string destinationPath)
    {
        using var image = await Image.LoadAsync(input);
        if (image.Width > MaxDimension || image.Height > MaxDimension)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(MaxDimension, MaxDimension)
            }));
        }
        var encoder = new JpegEncoder { Quality = JpegQuality };
        await image.SaveAsync(destinationPath, encoder);
    }
}

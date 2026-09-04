using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RekeszAppBackend.Migrations
{
    /// <inheritdoc />
    public partial class EladasHianyFelvasarlasKiegeszites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Az EladasTetelek.KifizetteHianyt (bool) helyett egy darabszám-mező kell:
            // hány hiányzó rekeszt fizettek ki készpénzben (0 = semennyit).
            migrationBuilder.DropColumn(
                name: "KifizetteHianyt",
                table: "EladasTetelek");

            migrationBuilder.AddColumn<int>(
                name: "HianyFizettDb",
                table: "EladasTetelek",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Maradék áru: előző napról átvitt tétel jelölése + opcionális felvásárlási fotó.
            migrationBuilder.AddColumn<bool>(
                name: "Athozott",
                table: "FelvasarlasTetelek",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KepUrl",
                table: "FelvasarlasTetelek",
                type: "longtext",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KepUrl",
                table: "FelvasarlasTetelek");

            migrationBuilder.DropColumn(
                name: "Athozott",
                table: "FelvasarlasTetelek");

            migrationBuilder.DropColumn(
                name: "HianyFizettDb",
                table: "EladasTetelek");

            migrationBuilder.AddColumn<bool>(
                name: "KifizetteHianyt",
                table: "EladasTetelek",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }
    }
}

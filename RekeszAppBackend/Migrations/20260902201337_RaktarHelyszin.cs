using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RekeszAppBackend.Migrations
{
    /// <inheritdoc />
    public partial class RaktarHelyszin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A kép mostantól zöldség (kategória) szintű, nem felvásárlási tételenkénti:
            // minden azonos zöldség ugyanazt a képet kapja, függetlenül attól, melyik
            // eladótól vettük. Ezért a KepUrl átkerül FelvasarlasTetelek-ről Zoldsegek-re.
            migrationBuilder.DropColumn(
                name: "KepUrl",
                table: "FelvasarlasTetelek");

            // Raktár funkció: egy felvásárlási tétel Kocsin (eladásra kész) vagy Raktárban
            // (fizikailag tárolva, még nem a kocsin) van. AthelyezveDb tárolja, hogy eddig
            // összesen hány db-ot helyeztek át az adott sorból a kocsira.
            migrationBuilder.AddColumn<int>(
                name: "AthelyezveDb",
                table: "FelvasarlasTetelek",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Helyszin",
                table: "FelvasarlasTetelek",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "KepUrl",
                table: "Zoldsegek",
                type: "longtext",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "KepUrl",
                table: "Zoldsegek");

            migrationBuilder.DropColumn(
                name: "Helyszin",
                table: "FelvasarlasTetelek");

            migrationBuilder.DropColumn(
                name: "AthelyezveDb",
                table: "FelvasarlasTetelek");

            migrationBuilder.AddColumn<string>(
                name: "KepUrl",
                table: "FelvasarlasTetelek",
                type: "longtext",
                nullable: true);
        }
    }
}

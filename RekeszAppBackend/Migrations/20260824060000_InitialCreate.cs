using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RekeszAppBackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Partnerek",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nev = table.Column<string>(type: "longtext", nullable: false),
                    Megjegyzes = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Partnerek", x => x.Id));

            migrationBuilder.CreateTable(
                name: "RekeszTipusok",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nev = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_RekeszTipusok", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Felhasznalonev = table.Column<string>(type: "longtext", nullable: false),
                    JelszoHash = table.Column<string>(type: "longtext", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Users", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Vevek",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nev = table.Column<string>(type: "longtext", nullable: true),
                    Megjegyzes = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_Vevek", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Zoldsegek",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nev = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    AlapertelmezettRekeszTipusId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Zoldsegek", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Zoldsegek_RekeszTipusok_AlapertelmezettRekeszTipusId",
                        column: x => x.AlapertelmezettRekeszTipusId,
                        principalTable: "RekeszTipusok",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FelvasarlasTetelek",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NapiSorszam = table.Column<int>(type: "int", nullable: false),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    Ido = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SajatTermek = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PartnerId = table.Column<int>(type: "int", nullable: true),
                    ZoldsegId = table.Column<int>(type: "int", nullable: false),
                    RekeszTipusId = table.Column<int>(type: "int", nullable: false),
                    Mennyiseg = table.Column<int>(type: "int", nullable: false),
                    Fizetve = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AdottRekeszDb = table.Column<int>(type: "int", nullable: false),
                    Egysegar = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Megjegyzes = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FelvasarlasTetelek", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FelvasarlasTetelek_Partnerek_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partnerek",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FelvasarlasTetelek_Zoldsegek_ZoldsegId",
                        column: x => x.ZoldsegId,
                        principalTable: "Zoldsegek",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FelvasarlasTetelek_RekeszTipusok_RekeszTipusId",
                        column: x => x.RekeszTipusId,
                        principalTable: "RekeszTipusok",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EladasTetelek",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    NapiSorszam = table.Column<int>(type: "int", nullable: false),
                    Datum = table.Column<DateOnly>(type: "date", nullable: false),
                    Ido = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    VevoId = table.Column<int>(type: "int", nullable: true),
                    ZoldsegId = table.Column<int>(type: "int", nullable: false),
                    RekeszTipusId = table.Column<int>(type: "int", nullable: false),
                    Mennyiseg = table.Column<int>(type: "int", nullable: false),
                    Fizetve = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Elvitte = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    VisszahozottDb = table.Column<int>(type: "int", nullable: false),
                    KifizetteHianyt = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Egysegar = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    Megjegyzes = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EladasTetelek", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EladasTetelek_Vevek_VevoId",
                        column: x => x.VevoId,
                        principalTable: "Vevek",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EladasTetelek_Zoldsegek_ZoldsegId",
                        column: x => x.ZoldsegId,
                        principalTable: "Zoldsegek",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EladasTetelek_RekeszTipusok_RekeszTipusId",
                        column: x => x.RekeszTipusId,
                        principalTable: "RekeszTipusok",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Felhasznalonev",
                table: "Users",
                column: "Felhasznalonev",
                unique: true)
                .Annotation("MySql:IndexPrefixLength", new[] { 191 });

            migrationBuilder.CreateIndex(
                name: "IX_Zoldsegek_Nev",
                table: "Zoldsegek",
                column: "Nev",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Zoldsegek_AlapertelmezettRekeszTipusId",
                table: "Zoldsegek",
                column: "AlapertelmezettRekeszTipusId");

            migrationBuilder.CreateIndex(
                name: "IX_RekeszTipusok_Nev",
                table: "RekeszTipusok",
                column: "Nev",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FelvasarlasTetelek_Datum",
                table: "FelvasarlasTetelek",
                column: "Datum");

            migrationBuilder.CreateIndex(
                name: "IX_FelvasarlasTetelek_PartnerId",
                table: "FelvasarlasTetelek",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_FelvasarlasTetelek_ZoldsegId",
                table: "FelvasarlasTetelek",
                column: "ZoldsegId");

            migrationBuilder.CreateIndex(
                name: "IX_FelvasarlasTetelek_RekeszTipusId",
                table: "FelvasarlasTetelek",
                column: "RekeszTipusId");

            migrationBuilder.CreateIndex(
                name: "IX_EladasTetelek_Datum",
                table: "EladasTetelek",
                column: "Datum");

            migrationBuilder.CreateIndex(
                name: "IX_EladasTetelek_VevoId",
                table: "EladasTetelek",
                column: "VevoId");

            migrationBuilder.CreateIndex(
                name: "IX_EladasTetelek_ZoldsegId",
                table: "EladasTetelek",
                column: "ZoldsegId");

            migrationBuilder.CreateIndex(
                name: "IX_EladasTetelek_RekeszTipusId",
                table: "EladasTetelek",
                column: "RekeszTipusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FelvasarlasTetelek");
            migrationBuilder.DropTable(name: "EladasTetelek");
            migrationBuilder.DropTable(name: "Users");
            migrationBuilder.DropTable(name: "Partnerek");
            migrationBuilder.DropTable(name: "Vevek");
            migrationBuilder.DropTable(name: "Zoldsegek");
            migrationBuilder.DropTable(name: "RekeszTipusok");
        }
    }
}

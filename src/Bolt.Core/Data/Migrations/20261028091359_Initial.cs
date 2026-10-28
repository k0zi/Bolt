using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bolt.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Apps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ForgeKind = table.Column<string>(type: "TEXT", nullable: false),
                    Host = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectPath = table.Column<string>(type: "TEXT", nullable: false),
                    RepoUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Author = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    BoltVersion = table.Column<string>(type: "TEXT", nullable: true),
                    IconPath = table.Column<string>(type: "TEXT", nullable: true),
                    HasCustomIcon = table.Column<bool>(type: "INTEGER", nullable: false),
                    LatestVersion = table.Column<string>(type: "TEXT", nullable: true),
                    LatestAssetName = table.Column<string>(type: "TEXT", nullable: true),
                    LatestAssetUrl = table.Column<string>(type: "TEXT", nullable: true),
                    LatestCheckedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    AddedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Apps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HostCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Host = table.Column<string>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HostCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Installations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StoreAppId = table.Column<int>(type: "INTEGER", nullable: false),
                    InstalledVersion = table.Column<string>(type: "TEXT", nullable: false),
                    PackageFormat = table.Column<string>(type: "TEXT", nullable: false),
                    PackageId = table.Column<string>(type: "TEXT", nullable: false),
                    UninstallCommand = table.Column<string>(type: "TEXT", nullable: true),
                    InstalledAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Installations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Installations_Apps_StoreAppId",
                        column: x => x.StoreAppId,
                        principalTable: "Apps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Apps_RepoUrl",
                table: "Apps",
                column: "RepoUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HostCredentials_Host",
                table: "HostCredentials",
                column: "Host",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Installations_StoreAppId",
                table: "Installations",
                column: "StoreAppId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HostCredentials");

            migrationBuilder.DropTable(
                name: "Installations");

            migrationBuilder.DropTable(
                name: "Apps");
        }
    }
}

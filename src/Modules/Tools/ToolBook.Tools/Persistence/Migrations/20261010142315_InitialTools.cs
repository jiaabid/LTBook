using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ToolBook.Tools.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialTools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tools");

            migrationBuilder.CreateTable(
                name: "Tools",
                schema: "tools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DefaultSlotMinutes = table.Column<int>(type: "int", nullable: false),
                    TypicalBookingMinutes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tools", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "tools",
                table: "Tools",
                columns: new[] { "Id", "DefaultSlotMinutes", "Description", "IsActive", "Location", "Name", "Type", "TypicalBookingMinutes" },
                values: new object[,]
                {
                    { new Guid("0199c3a0-0000-7000-8000-000000000001"), 60, "Field-emission SEM for high-resolution surface imaging. Secondary and backscattered electron detectors, EDX available.", true, "Cleanroom B, Room 1.12", "Scanning Electron Microscope", "Microscopy", 120 },
                    { new Guid("0199c3a0-0000-7000-8000-000000000002"), 30, "Variable-angle spectroscopic ellipsometer for thin-film thickness and optical constants (190–1700 nm).", true, "Lab 2.04", "Spectroscopic Ellipsometer", "Optical metrology", 45 },
                    { new Guid("0199c3a0-0000-7000-8000-000000000003"), 60, "Four-probe manual station with a semiconductor parameter analyzer for I–V and C–V measurements.", true, "Lab 2.10", "Probe Station", "Electrical characterization", 90 },
                    { new Guid("0199c3a0-0000-7000-8000-000000000004"), 60, "AFM with tapping and contact modes for nanoscale topography and roughness measurements.", true, "Lab 1.07", "Atomic Force Microscope", "Microscopy", 120 },
                    { new Guid("0199c3a0-0000-7000-8000-000000000005"), 30, "RTA for wafers up to 150 mm, up to 1200 °C under N2, Ar or forming gas.", true, "Cleanroom A, Room 0.03", "Rapid Thermal Annealer", "Thermal processing", 40 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tools_Name",
                schema: "tools",
                table: "Tools",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tools",
                schema: "tools");
        }
    }
}

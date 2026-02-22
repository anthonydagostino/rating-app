using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RatingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyToAttractivenessRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop RatingDetails first (has FK to RatingCriteria)
            migrationBuilder.DropTable(name: "RatingDetails");

            migrationBuilder.DropTable(name: "RatingCriteria");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RatingCriteria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingCriteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RatingDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RatingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CriterionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatingDetails_RatingCriteria_CriterionId",
                        column: x => x.CriterionId,
                        principalTable: "RatingCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RatingDetails_Ratings_RatingId",
                        column: x => x.RatingId,
                        principalTable: "Ratings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RatingDetails_CriterionId",
                table: "RatingDetails",
                column: "CriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_RatingDetails_RatingId_CriterionId",
                table: "RatingDetails",
                columns: new[] { "RatingId", "CriterionId" },
                unique: true);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RatingApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiCriteriaRatings : Migration
    {
        private static readonly Guid SkillCriterionId = new("00000000-0000-0000-0000-000000000001");
        private static readonly Guid CommCriterionId = new("00000000-0000-0000-0000-000000000002");
        private static readonly Guid CultureCriterionId = new("00000000-0000-0000-0000-000000000003");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Comment column to Ratings
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Ratings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Create RatingCriteria table
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

            // Create RatingDetails table
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
                name: "IX_RatingDetails_RatingId_CriterionId",
                table: "RatingDetails",
                columns: new[] { "RatingId", "CriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatingDetails_CriterionId",
                table: "RatingDetails",
                column: "CriterionId");

            // Seed default criteria
            migrationBuilder.InsertData(
                table: "RatingCriteria",
                columns: new[] { "Id", "Name", "Weight", "IsRequired", "IsActive" },
                values: new object[,]
                {
                    { SkillCriterionId,   "Skill",         0.40, true,  true },
                    { CommCriterionId,    "Communication", 0.35, true,  true },
                    { CultureCriterionId, "Culture",       0.25, false, true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RatingDetails");
            migrationBuilder.DropTable(name: "RatingCriteria");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Ratings");
        }
    }
}
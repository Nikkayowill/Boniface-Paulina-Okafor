using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Okafor_.NET.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPushSubscriptionCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    EndpointHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    P256DH = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Auth = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSuccessAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFailureAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushSubscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_EndpointHash",
                table: "PushSubscriptions",
                column: "EndpointHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_UserId",
                table: "PushSubscriptions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PushSubscriptions");
        }
    }
}

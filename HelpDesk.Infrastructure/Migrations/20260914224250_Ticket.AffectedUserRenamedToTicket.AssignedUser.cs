using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HelpDesk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TicketAffectedUserRenamedToTicketAssignedUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Users_AffectedUserId",
                table: "Tickets");

            migrationBuilder.RenameColumn(
                name: "AffectedUserId",
                table: "Tickets",
                newName: "AssignedUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_AffectedUserId",
                table: "Tickets",
                newName: "IX_Tickets_AssignedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_AssignedUserId",
                table: "Tickets",
                column: "AssignedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Users_AssignedUserId",
                table: "Tickets");

            migrationBuilder.RenameColumn(
                name: "AssignedUserId",
                table: "Tickets",
                newName: "AffectedUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Tickets_AssignedUserId",
                table: "Tickets",
                newName: "IX_Tickets_AffectedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Users_AffectedUserId",
                table: "Tickets",
                column: "AffectedUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

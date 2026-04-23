using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Zebrahoof_EMR.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationUserExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MfaSecret",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordSalt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Scope = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Department = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    DeviceName = table.Column<string>(type: "TEXT", nullable: true),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "role-admin", "d12c914d-0bec-47d4-826f-fc97787e3d19", "Admin", "ADMIN" },
                    { "role-billing", "fecf3e9f-f94a-4f1a-9a5f-315aa8731fb8", "Billing", "BILLING" },
                    { "role-lab", "d421a4e0-7dab-4bfc-80ec-2757f6f1c7bd", "Lab", "LAB" },
                    { "role-ma", "8f626ab8-2bdc-47ea-8a8d-615dcd7eaf97", "MA", "MA" },
                    { "role-nurse", "96a9e87a-2b5d-436b-b582-aaf7a0a4bf69", "Nurse", "NURSE" },
                    { "role-patient", "c0ca988d-fec0-4eda-8b87-b8f89b4d9c43", "Patient", "PATIENT" },
                    { "role-physician", "eaa790d8-4a99-4afd-8bf5-f8795ca3e750", "Physician", "PHYSICIAN" },
                    { "role-scheduler", "3261eab5-0286-413c-9c9a-ea1ad4f24b6b", "Scheduler", "SCHEDULER" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "CreatedAt", "DateOfBirth", "Department", "DisplayName", "Email", "EmailConfirmed", "FirstName", "IsActive", "LastName", "LockoutEnabled", "LockoutEnd", "MfaSecret", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PasswordSalt", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[,]
                {
                    { "Admin", 0, "5e12719c-aad1-490c-bee9-9b9d3c3ae9eb", new DateTime(2026, 1, 1, 19, 27, 35, 796, DateTimeKind.Utc).AddTicks(3888), null, "Administration", "System Administrator", "admin@zebrahoof.com", true, null, true, null, false, null, null, "ADMIN@ZEBRAHOOF.COM", "ADMIN", "AQAAAAIAAYagAAAAEHaG7ieu1XMPsbb7ticZndGfu7UWgYlyhUbu1iM34xre4wxnK8eC6+bIvPy194kMJQ==", null, null, false, "01276d82-75c4-4277-a97e-1418d7ca64dd", false, "Admin" },
                    { "Billing", 0, "e17a1930-cbec-404a-98e7-3c85ff604f63", new DateTime(2026, 1, 1, 19, 27, 35, 999, DateTimeKind.Utc).AddTicks(9328), null, "Revenue Cycle", "Billing Specialist", "billing@zebrahoof.com", true, null, true, null, false, null, null, "BILLING@ZEBRAHOOF.COM", "BILLING", "AQAAAAIAAYagAAAAEFLWpL+bxzDIVbIbRlXaja6JDvqC1MRtIwLOjW3s+MneFq/4eo2IVl0tdRrTe8+uBA==", null, null, false, "c7cb0732-b9a0-4bae-90c7-d038e21a2216", false, "Billing" },
                    { "Lab", 0, "f9269baa-dfcd-41f3-9665-bd15137b1d3e", new DateTime(2026, 1, 1, 19, 27, 36, 103, DateTimeKind.Utc).AddTicks(6882), null, "Diagnostics", "Lab Technician", "lab@zebrahoof.com", true, null, true, null, false, null, null, "LAB@ZEBRAHOOF.COM", "LAB", "AQAAAAIAAYagAAAAEFysa7VKxG9pf4Ua5wwIF5j4vZXUynCSGEogfcsX6l1xc9My7LQoEkoCGBBlfq2v2w==", null, null, false, "1251368e-7c93-415b-ae1f-d4c137bf2cbb", false, "Lab" },
                    { "MA", 0, "128a7ece-b689-4c7f-8269-1cffc03343fa", new DateTime(2026, 1, 1, 19, 27, 35, 947, DateTimeKind.Utc).AddTicks(8882), null, "Clinical Support", "Medical Assistant", "ma@zebrahoof.com", true, null, true, null, false, null, null, "MA@ZEBRAHOOF.COM", "MA", "AQAAAAIAAYagAAAAEAyeoZSklz8Ow+Ivr3zdOlplX9dc6Yvj0cOELg6WkG88dlLtklP9zwVK4VAvP//B6Q==", null, null, false, "5fcef561-99e5-4b34-9d14-eb058cfb8bd7", false, "MA" },
                    { "Nurse", 0, "51fd6800-eaca-4384-a451-b7cadee43a98", new DateTime(2026, 1, 1, 19, 27, 35, 897, DateTimeKind.Utc).AddTicks(1867), null, "Clinical", "Charge Nurse", "nurse@zebrahoof.com", true, null, true, null, false, null, null, "NURSE@ZEBRAHOOF.COM", "NURSE", "AQAAAAIAAYagAAAAEBykJfKuEB0Cks/qtyCL7jRFUekYIVjCQsky0yWAFSdVx0JrOsDDOrdPwNCjZgqfwQ==", null, null, false, "4c1e648d-229e-46e0-b8e4-6e4b1b3ad100", false, "Nurse" },
                    { "Patient", 0, "7f00d42a-ad2e-42cf-8c8d-b25bfd10e2b2", new DateTime(2026, 1, 1, 19, 27, 36, 155, DateTimeKind.Utc).AddTicks(1998), null, "Patient Portal", "Sample Patient", "patient@zebrahoof.com", true, null, true, null, false, null, null, "PATIENT@ZEBRAHOOF.COM", "PATIENT", "AQAAAAIAAYagAAAAEIO7/gC2XsDWiMS6ENNbKS99Gpy91Qywo646HzNfq1Y2fdY2H2lrmSUoGi0rS47C4g==", null, null, false, "4932699b-e23a-4d78-887e-28a96d70d1a5", false, "Patient" },
                    { "Physician", 0, "77467bbe-969d-4158-a6f1-1ebb00746245", new DateTime(2026, 1, 1, 19, 27, 35, 847, DateTimeKind.Utc).AddTicks(374), null, "Clinical", "Attending Physician", "physician@zebrahoof.com", true, null, true, null, false, null, null, "PHYSICIAN@ZEBRAHOOF.COM", "PHYSICIAN", "AQAAAAIAAYagAAAAEPxh8GOk/GMHeyRrs1KVR5xwdQel7J8k6+wMJINHps1ulPp31+fZJ9CR+fhYBw/IoQ==", null, null, false, "b9e6cbf6-963e-4af4-a255-b46cbdd13f37", false, "Physician" },
                    { "Scheduler", 0, "00f891c4-eb3c-4e30-ac2f-5274327313ce", new DateTime(2026, 1, 1, 19, 27, 36, 51, DateTimeKind.Utc).AddTicks(3660), null, "Operations", "Scheduling Coordinator", "scheduler@zebrahoof.com", true, null, true, null, false, null, null, "SCHEDULER@ZEBRAHOOF.COM", "SCHEDULER", "AQAAAAIAAYagAAAAEC/HdRlmyhMx8F/oZcM+0zDG0SEncns9gQ1AW69IDuqj26lwAHT1hSIvDYZjRPOUkg==", null, null, false, "929e923f-a152-4525-9a03-6cba23ca4d37", false, "Scheduler" }
                });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { "role-admin", "Admin" },
                    { "role-billing", "Billing" },
                    { "role-lab", "Lab" },
                    { "role-ma", "MA" },
                    { "role-nurse", "Nurse" },
                    { "role-patient", "Patient" },
                    { "role-physician", "Physician" },
                    { "role-scheduler", "Scheduler" }
                });

            migrationBuilder.InsertData(
                table: "UserProfiles",
                columns: new[] { "UserId", "Department", "DisplayName" },
                values: new object[,]
                {
                    { "Admin", "Administration", "System Administrator" },
                    { "Billing", "Revenue Cycle", "Billing Specialist" },
                    { "Lab", "Diagnostics", "Lab Technician" },
                    { "MA", "Clinical Support", "Medical Assistant" },
                    { "Nurse", "Clinical", "Charge Nurse" },
                    { "Patient", "Patient Portal", "Sample Patient" },
                    { "Physician", "Clinical", "Attending Physician" },
                    { "Scheduler", "Operations", "Scheduling Coordinator" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId_ExpiresAt",
                table: "UserSessions",
                columns: new[] { "UserId", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "role-admin", "Admin" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "role-billing", "Billing" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "role-lab", "Lab" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "role-ma", "MA" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "role-nurse", "Nurse" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "role-patient", "Patient" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "role-physician", "Physician" });

            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "role-scheduler", "Scheduler" });

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-admin");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-billing");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-lab");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-ma");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-nurse");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-patient");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-physician");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-scheduler");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Admin");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Billing");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Lab");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "MA");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Nurse");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Patient");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Physician");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Scheduler");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MfaSecret",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PasswordSalt",
                table: "AspNetUsers");
        }
    }
}

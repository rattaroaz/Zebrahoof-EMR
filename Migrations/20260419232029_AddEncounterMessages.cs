using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zebrahoof_EMR.Migrations
{
    /// <inheritdoc />
    public partial class AddEncounterMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EncounterMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    UserInput = table.Column<string>(type: "TEXT", nullable: false),
                    GrokResponse = table.Column<string>(type: "TEXT", nullable: false),
                    IncludedHistory = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EncounterMessages", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-admin",
                column: "ConcurrencyStamp",
                value: "8f29bbc9-4a43-48cc-a4d3-a7f64ecb0041");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-billing",
                column: "ConcurrencyStamp",
                value: "119991bb-da87-4464-9e89-daecd9afe132");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-lab",
                column: "ConcurrencyStamp",
                value: "401ab706-701c-45df-a8f4-53ddde663c4d");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-ma",
                column: "ConcurrencyStamp",
                value: "c8fd2a79-45f2-4076-affa-144d3485612f");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-nurse",
                column: "ConcurrencyStamp",
                value: "2c1f032b-d5e0-4b06-8ec5-620a05855aea");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-patient",
                column: "ConcurrencyStamp",
                value: "14ec72f0-3598-47e3-8221-b4226c7e5fdd");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-physician",
                column: "ConcurrencyStamp",
                value: "ca262c94-1cb9-4bff-9a9b-f88ea8528516");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-scheduler",
                column: "ConcurrencyStamp",
                value: "c36c93a9-f674-4347-9f43-071b1d7330d0");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Admin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "35a9c835-5446-413c-944a-0b9cc6b2f81f", new DateTime(2026, 4, 19, 23, 20, 28, 877, DateTimeKind.Utc).AddTicks(9119), "AQAAAAIAAYagAAAAEKzu6xEoPoDkI9pcgSLNrHCeUGCty28+HkKChDZ0n8NyvflxWJwc+1e3EnwpAl5Qvw==", "bb16ff86-7987-4fc0-9173-34d49aa2f456" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Billing",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "e49bc192-c832-4250-a9b1-150c7d6986e7", new DateTime(2026, 4, 19, 23, 20, 29, 86, DateTimeKind.Utc).AddTicks(7991), "AQAAAAIAAYagAAAAEJGVw7Xx85KCB8pmwzMw3UGOLm6XiWmvgvq/Db+6Z0jDgYdg8zAsP48pCVEas7Q1dg==", "47c037ab-6f49-4392-8ab9-d1e0c27424e8" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Lab",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a9574389-f46a-4788-b612-65c28599786a", new DateTime(2026, 4, 19, 23, 20, 29, 189, DateTimeKind.Utc).AddTicks(5167), "AQAAAAIAAYagAAAAEBxoLfcgo+cT8BqayEkA+bwRrrK6yRghClmFV+0opFu8DGPhBSnwEg58n+C64EGrjA==", "95633fe2-5781-4106-8d3b-e454a21f2f1c" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "MA",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "66c6394a-8b6d-4c4c-9398-ed1cc7fd10a2", new DateTime(2026, 4, 19, 23, 20, 29, 34, DateTimeKind.Utc).AddTicks(5633), "AQAAAAIAAYagAAAAEPqhXyH8Q104uLy4XpGk//OlezTIpRFuH8EzlBvzFWcqDbLk1D7osAFubjm7WRcfWw==", "86a097a9-4932-4a38-95c2-b1b050fdbe83" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Nurse",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a89fdd1f-c12f-4b82-bb39-f886ed309500", new DateTime(2026, 4, 19, 23, 20, 28, 980, DateTimeKind.Utc).AddTicks(5538), "AQAAAAIAAYagAAAAEI3Mq27fq2WLl/Iw0JFV95NBDmi0SSYO6IubUBlNW9eNBC/3nwa7K341ObDug2tILw==", "536b45bf-d0d0-43e1-8f2b-addc1a34a6bc" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Patient",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a8942ce4-a0d7-4e0a-b1e9-d14984259022", new DateTime(2026, 4, 19, 23, 20, 29, 241, DateTimeKind.Utc).AddTicks(345), "AQAAAAIAAYagAAAAEIiVuLXGTqLsCi3QkjlOZFPTP5kSQ3WhzrqWPHt4MWOu9NbrI4qS3wzIlxYzUYAXSw==", "31df86ac-abe3-4304-b458-e666382f8ca2" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Physician",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "aa9adfdc-954a-4e1b-a0dc-f3d6cc116932", new DateTime(2026, 4, 19, 23, 20, 28, 928, DateTimeKind.Utc).AddTicks(6665), "AQAAAAIAAYagAAAAEL9+hOGTQXiveehArWts8z/WmrCUHBjg8JKQewm2t5TU7wTwmHtVWi78YmqDmmx5jg==", "01523c76-8873-4d27-82c2-3d909b2a97b7" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Scheduler",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "46b32db7-ae35-43db-afbf-466c8984c35b", new DateTime(2026, 4, 19, 23, 20, 29, 138, DateTimeKind.Utc).AddTicks(5028), "AQAAAAIAAYagAAAAEJFJflrcMdY1vkpFoQA33xmcllJgT39OzAcAFGpuWJjHiRYAi1oMoVwI96qjV03rBA==", "5617e609-e4c6-4465-8ed7-ccbd9de8a8cc" });

            migrationBuilder.CreateIndex(
                name: "IX_EncounterMessages_PatientId_CreatedAt",
                table: "EncounterMessages",
                columns: new[] { "PatientId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EncounterMessages");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-admin",
                column: "ConcurrencyStamp",
                value: "ccfc17ad-4b8e-426d-ab32-3a9056a514b0");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-billing",
                column: "ConcurrencyStamp",
                value: "882dd874-038f-45e8-be46-84c6cc25909a");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-lab",
                column: "ConcurrencyStamp",
                value: "0540aef8-ae91-4a59-91fe-74f831ef423c");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-ma",
                column: "ConcurrencyStamp",
                value: "40fd6c09-0281-4373-bb6d-ca88cd7a93ad");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-nurse",
                column: "ConcurrencyStamp",
                value: "7bcdf805-9162-495b-aa40-85570b402407");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-patient",
                column: "ConcurrencyStamp",
                value: "5edd471c-1774-4384-a5e6-abc143ac3fff");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-physician",
                column: "ConcurrencyStamp",
                value: "48b2206e-88db-46a7-b1e2-95d961291e50");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-scheduler",
                column: "ConcurrencyStamp",
                value: "ca863e36-f66f-407b-8448-6f254c944408");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Admin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "4b6b735e-4493-4693-b8b8-a5711e34f5b8", new DateTime(2026, 4, 19, 4, 18, 7, 790, DateTimeKind.Utc).AddTicks(4678), "AQAAAAIAAYagAAAAEH8Z4qE6qnVnJfxOIzkd1TwFlb6Rth/iMuzpPqg2Tm6u8JNhnaMf1Bt9oQq+cvrPYg==", "aa62aea0-a979-4e4a-ad69-a1f0815c6d52" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Billing",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "3aaef4bb-1b1b-410d-a142-d02905513326", new DateTime(2026, 4, 19, 4, 18, 8, 1, DateTimeKind.Utc).AddTicks(3931), "AQAAAAIAAYagAAAAEGNt3tWSTRYjoTVErSE6UuxeEY4G+CUj+PE3IGPgMcLuilCf2tIMubNNqlC2zLIQwQ==", "f7d46530-e21e-4b28-af17-a5f4cce6b92a" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Lab",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "3f5f9df3-75a5-429d-87a6-14b0cec07a4b", new DateTime(2026, 4, 19, 4, 18, 8, 119, DateTimeKind.Utc).AddTicks(9553), "AQAAAAIAAYagAAAAEIV1eUYg8Wff78ZqXs9YNXKlJfZPvG1XeuTvHA4ooBVsffQxznY7DHZXYXYomt/xmg==", "cbb35dd6-1cd3-4b16-9c44-61dead848a09" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "MA",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "edf61e71-f71a-4f8f-a7ae-a174f9a4ca98", new DateTime(2026, 4, 19, 4, 18, 7, 949, DateTimeKind.Utc).AddTicks(5956), "AQAAAAIAAYagAAAAELEVqJoPCTXeY75sEL9mz9A/mxRId5tooSeh/6h2KUWl5L6xqCkKpPO71eFaa+kw/A==", "37ab34c6-6613-4bad-9366-e5f136f258ec" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Nurse",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "9555e825-f43e-4a7d-8fb3-0ec739797ed2", new DateTime(2026, 4, 19, 4, 18, 7, 897, DateTimeKind.Utc).AddTicks(7009), "AQAAAAIAAYagAAAAEGdfYRep4Hn522SQkn8mJY5FVrGDMZ8QnnrqRWbG3Ho4Rly5O2wRpn7KYl/KtbF2Sw==", "64dcf61f-fc30-4636-ada8-b842afd9c5d1" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Patient",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f24ed4fe-2320-4cc3-8a47-1ac9abae7be6", new DateTime(2026, 4, 19, 4, 18, 8, 181, DateTimeKind.Utc).AddTicks(5714), "AQAAAAIAAYagAAAAEJO4PylkicN47DQvjxP5hK/YRBQCVb8a6FipL68/3moi2VexGR5Qnsq2aGvS0zF6vw==", "0284172a-d560-4d94-8c53-d862a92fc412" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Physician",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "50f7c824-d006-42ce-a59d-ab66914bc91f", new DateTime(2026, 4, 19, 4, 18, 7, 841, DateTimeKind.Utc).AddTicks(1728), "AQAAAAIAAYagAAAAEJn51BRYN8wO3Nk5BKFrvT6aXYYnCF6KzZOhPmpZOFVRrkF+E6wbzyHsL52Au1y1uw==", "3c2f3af9-e7f7-4d84-9898-3607ccb7701c" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Scheduler",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "3c2a6e75-6304-40d0-abe0-926547785df5", new DateTime(2026, 4, 19, 4, 18, 8, 54, DateTimeKind.Utc).AddTicks(2613), "AQAAAAIAAYagAAAAEB2bO4qLW2LhW2xMPAGjjbD0i7DCZ8AW9EqE54ynYWkOOFyMV79RufCEzg4u8ZRO0g==", "00d6ac4f-0c00-4433-a8df-5c95c701a06e" });
        }
    }
}

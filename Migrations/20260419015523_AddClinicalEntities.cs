using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zebrahoof_EMR.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExtractedText",
                table: "Documents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Allergies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Allergen = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Reaction = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    OnsetDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allergies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Dose = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Route = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Frequency = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Prescriber = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsHighRisk = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLongTerm = table.Column<bool>(type: "INTEGER", nullable: false),
                    Instructions = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    RefillsRemaining = table.Column<int>(type: "INTEGER", nullable: true),
                    DaysSupply = table.Column<int>(type: "INTEGER", nullable: true),
                    Pharmacy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Problems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IcdCode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    OnsetDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Problems", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-admin",
                column: "ConcurrencyStamp",
                value: "77491d8d-d594-4a04-908a-7a7afcdfa058");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-billing",
                column: "ConcurrencyStamp",
                value: "d87cd4bc-c737-4b85-ad82-1fc22077eb41");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-lab",
                column: "ConcurrencyStamp",
                value: "3817098a-4ed7-4357-bc96-53f551aeee77");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-ma",
                column: "ConcurrencyStamp",
                value: "464f63b5-19a5-473e-9516-42ee5bf90a5c");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-nurse",
                column: "ConcurrencyStamp",
                value: "c4b2a4db-13f2-459e-96fc-c0a772ef12b6");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-patient",
                column: "ConcurrencyStamp",
                value: "fa8b9545-e6c6-471c-9969-2e044e1d444e");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-physician",
                column: "ConcurrencyStamp",
                value: "4c3a1a4d-770d-4eec-82aa-58b06dc58726");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-scheduler",
                column: "ConcurrencyStamp",
                value: "d272fe06-4f71-42b7-bfe9-b8741647545e");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Admin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "1bea4de8-7305-44e9-a953-d5786e548809", new DateTime(2026, 4, 19, 1, 55, 22, 192, DateTimeKind.Utc).AddTicks(3195), "AQAAAAIAAYagAAAAEENskPoyaA/46i8BCctjINXCyFKUsG2iS6rU/tfOhUGm87nAQGnf3N/T4E4HTUZDew==", "8f6b0383-929d-4848-85d4-6541ecb85b30" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Billing",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "7232813a-2b2d-49c3-b7c4-3af38e9afa2c", new DateTime(2026, 4, 19, 1, 55, 22, 411, DateTimeKind.Utc).AddTicks(8031), "AQAAAAIAAYagAAAAEIlWNbAUR1VLvgwjeEP9rDoK/RnfECUVnMzAVgxuhmX/hoqPrIGpIkMRS/7S6/OU3g==", "e60e84c7-1b0c-4a13-8594-7081eb82e1c8" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Lab",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "ed747eaa-474f-40d5-a3f1-de00f5be54aa", new DateTime(2026, 4, 19, 1, 55, 22, 517, DateTimeKind.Utc).AddTicks(7919), "AQAAAAIAAYagAAAAEDVL3liJcI90p+Id24V8T7VEMrOlIINX/1Lzf27WQLgOXsbthCOHPQlHCzRXALMqeQ==", "f2f8126b-5ce7-409b-926d-2e01b601427e" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "MA",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "d9fd5c9e-df09-4db4-a060-9a5c8190e6cb", new DateTime(2026, 4, 19, 1, 55, 22, 358, DateTimeKind.Utc).AddTicks(1517), "AQAAAAIAAYagAAAAEGwbfpmBl0BuuaPZ2eouLOs9nDqSTZWO5K38ZkYivkHg/wE7poNvpH7QJC3behZwow==", "2107d36e-6a8f-44f9-bbff-73ed1c10ec6b" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Nurse",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "b8cb823c-6675-456a-b805-583f54728744", new DateTime(2026, 4, 19, 1, 55, 22, 297, DateTimeKind.Utc).AddTicks(5929), "AQAAAAIAAYagAAAAENxXOHDfiHfvEWE3GycmllTaZ/umi88UPttcEeC/4gbcWPlUsFJPmQxXb+EF9oZ9rw==", "7eb5d03a-1189-43b6-b4c4-2e5ddd8dedf0" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Patient",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "5e260e6c-f064-4aaa-ac45-9a9799a970b5", new DateTime(2026, 4, 19, 1, 55, 22, 582, DateTimeKind.Utc).AddTicks(2083), "AQAAAAIAAYagAAAAEObSpTGORbOkG3b5RM2fdVYDaHpGXv/hEtN9im/2JIJHflQzNVlcUyfJH8bwPxWBpA==", "e44e0f45-942a-4c3b-83e1-fe2d1fe777d4" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Physician",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "2bc23c16-c2bc-4f93-a9c7-9a5957062b74", new DateTime(2026, 4, 19, 1, 55, 22, 244, DateTimeKind.Utc).AddTicks(6352), "AQAAAAIAAYagAAAAELTjl3/quA5wnvB0j8AnKYR3VGwsyb7S+igDMFm86vk/BN1cofGiVPNiDg2F6hGjGA==", "c3e816f1-93a4-40ec-b330-af4cfa16db48" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Scheduler",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "7ce23d30-2852-4762-a936-cd8af237a3e9", new DateTime(2026, 4, 19, 1, 55, 22, 465, DateTimeKind.Utc).AddTicks(2412), "AQAAAAIAAYagAAAAEEOOGkGlKq8NqS1PMKnG4LJ6Qq44DO2Rmdw9wsehC5SReTDnmyivnuSVqk0IxBR8Cw==", "ac18e2d5-b258-4501-b1ce-b9ec109989f6" });

            migrationBuilder.CreateIndex(
                name: "IX_Allergies_PatientId",
                table: "Allergies",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Medications_PatientId",
                table: "Medications",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_PatientId",
                table: "Problems",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Allergies");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "Problems");

            migrationBuilder.DropColumn(
                name: "ExtractedText",
                table: "Documents");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-admin",
                column: "ConcurrencyStamp",
                value: "ffd7324e-83ef-4e6c-a6e2-e538a26a6d83");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-billing",
                column: "ConcurrencyStamp",
                value: "3a328db7-31dc-4c95-a569-10edc8652175");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-lab",
                column: "ConcurrencyStamp",
                value: "55e60245-7996-4993-8d47-222c2ff517c4");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-ma",
                column: "ConcurrencyStamp",
                value: "320631a0-4928-44d2-860e-323a04ed11ec");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-nurse",
                column: "ConcurrencyStamp",
                value: "302c58e1-be31-4267-9ab7-a0a94df2be5c");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-patient",
                column: "ConcurrencyStamp",
                value: "c29f3eaa-cd60-4207-95a1-eb8f006f8381");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-physician",
                column: "ConcurrencyStamp",
                value: "5d7e9380-234b-436e-add8-36b51a1d226c");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-scheduler",
                column: "ConcurrencyStamp",
                value: "c0cf3243-ea83-4108-9e5b-4ec20027a12f");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Admin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f2bd58c6-8abf-4ce8-8ee5-3e19740b0c15", new DateTime(2026, 1, 9, 5, 44, 26, 90, DateTimeKind.Utc).AddTicks(6239), "AQAAAAIAAYagAAAAEJfeCWV8RKT1rPvJHMG78+DlnpD9IQi4ll6/m2AadrPgmKcNzaBusAbiXIju9a7mnw==", "34ac99d2-3d05-4a5f-acca-d51c3dd5f8b9" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Billing",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "8241825e-99e6-473e-b6c8-316314fa7350", new DateTime(2026, 1, 9, 5, 44, 26, 294, DateTimeKind.Utc).AddTicks(3782), "AQAAAAIAAYagAAAAEBlaE8zW9BWaxAPUQQpjEJBXdIl7BTTP6Qf1VFAu1Z2JGLK1rmVQDHso7+rPQByUGQ==", "e4db29d9-6c86-4a5c-8326-736c48e26e1f" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Lab",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "0cb32a49-6fd4-470c-812e-2f9bf6a90b9c", new DateTime(2026, 1, 9, 5, 44, 26, 396, DateTimeKind.Utc).AddTicks(1085), "AQAAAAIAAYagAAAAEDe0gJT9NGgnMxNvVnbTOECCCwpccr5ur+aIlrdDn1qDcINxkYbvly9Axnh/EhJw6g==", "9386893c-970f-4e03-8e47-e4e6366afcd9" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "MA",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "5fe37148-5447-4a95-a108-eeab07b23635", new DateTime(2026, 1, 9, 5, 44, 26, 239, DateTimeKind.Utc).AddTicks(9319), "AQAAAAIAAYagAAAAEBX4C3PXd3xCIwi9vRlhzBHJ97/YtWY3+t0UWFQzmlKLcQYHAMjUmi4LwKce5loE+A==", "fc753c12-17df-4291-b7a7-5d5fd4ac7843" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Nurse",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "2e85c72c-79a0-4bc7-ae3b-d9e3de0e8316", new DateTime(2026, 1, 9, 5, 44, 26, 190, DateTimeKind.Utc).AddTicks(5454), "AQAAAAIAAYagAAAAEKLbwZv6b38QWhte73EAiXztg4DOo50IwAT/igzsAKKwuKOGlALgdFkcrzUHpOOmQA==", "c206baaa-4fc2-4b28-a05b-4e088f156b23" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Patient",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f4c59a32-60bc-4f1e-9be9-3fd1a72e10de", new DateTime(2026, 1, 9, 5, 44, 26, 448, DateTimeKind.Utc).AddTicks(7536), "AQAAAAIAAYagAAAAEH7Mwzna9W8bGLRFB8mF9oXtixbdcGechKxVxStfIY643K0ONJMxCH6p4JLrYZtf5w==", "64587f76-f6b1-44bc-9e1f-3928c586eafc" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Physician",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "2b540f32-903b-4284-8c32-5861ddcabfcf", new DateTime(2026, 1, 9, 5, 44, 26, 140, DateTimeKind.Utc).AddTicks(1250), "AQAAAAIAAYagAAAAEH8nNSFD8AK58LXL+ICLg67J9zZfTekuYHacbDNHW5SPQITf1o2FDpD/vKC9bxaK5A==", "f79ccbc5-fc49-429c-a9fa-171a6898f40d" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Scheduler",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "6e46b3b5-bb91-4781-893d-3c70842e3813", new DateTime(2026, 1, 9, 5, 44, 26, 345, DateTimeKind.Utc).AddTicks(4059), "AQAAAAIAAYagAAAAECKiIfzfalDBbOvCv2RjKVMoVuhoEdqH1eCkrYKJQ9lwjJzBcNTWN4gPwRXlNHGpTw==", "4df660cf-d02f-4a03-9b76-128be13165c5" });
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zebrahoof_EMR.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientStickyNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PatientStickyNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    X = table.Column<double>(type: "REAL", nullable: false),
                    Y = table.Column<double>(type: "REAL", nullable: false),
                    IsVisible = table.Column<bool>(type: "INTEGER", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientStickyNotes", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_PatientStickyNotes_UserId_PatientId",
                table: "PatientStickyNotes",
                columns: new[] { "UserId", "PatientId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientStickyNotes");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-admin",
                column: "ConcurrencyStamp",
                value: "11ed5170-0e22-4423-a2cd-5b3d55d8abe1");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-billing",
                column: "ConcurrencyStamp",
                value: "f3718185-592b-4e0b-9bdc-589e2831243e");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-lab",
                column: "ConcurrencyStamp",
                value: "b4d4d356-2019-4192-bdb7-798d3eb85c5f");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-ma",
                column: "ConcurrencyStamp",
                value: "f10ae219-4c14-4343-8ce3-a1f6ec29fa76");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-nurse",
                column: "ConcurrencyStamp",
                value: "b9eba352-4820-4048-8757-e6763b3ebe4c");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-patient",
                column: "ConcurrencyStamp",
                value: "8cff3eb3-569f-4572-8b85-9197c22787cd");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-physician",
                column: "ConcurrencyStamp",
                value: "38e1e969-9e13-4ea0-95fd-b51a3c6db920");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-scheduler",
                column: "ConcurrencyStamp",
                value: "c9836d02-6517-4c41-a079-aa929feb4024");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Admin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "7c383c23-a550-4d5e-9104-ba4d3ce3c415", new DateTime(2026, 1, 9, 5, 30, 31, 28, DateTimeKind.Utc).AddTicks(759), "AQAAAAIAAYagAAAAEN1fCQ0jHgpzJQeDZVFYjEX9/QJz0j7KymsQ8AXOGOT/Z6z6uWsUfujjKspedWiuIQ==", "eb09ebb7-7c42-4429-8b3b-a03717a0a836" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Billing",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "670abb40-ecd4-454d-bb10-7d32626e8574", new DateTime(2026, 1, 9, 5, 30, 31, 229, DateTimeKind.Utc).AddTicks(5708), "AQAAAAIAAYagAAAAEMQMajWScOiK52mNo4qimQELUz75UQN6tY5Njy1s4NTtQIJHUz7KmabB7VN6lsHSlA==", "5c529c59-8491-4d21-92ad-32402c1287c5" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Lab",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a2bb0474-0b5e-44d2-b828-e53593d36872", new DateTime(2026, 1, 9, 5, 30, 31, 337, DateTimeKind.Utc).AddTicks(5569), "AQAAAAIAAYagAAAAEAlsqe4R2jut1l1IhaELNWURAUgB8O5SAfxB6iccY2RlcShRWVg81tV5us2pWYEa9w==", "994f99b6-f124-4df4-905b-f7ca8e6746b7" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "MA",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a24118f8-f1c5-4900-823b-5f46c856fb1d", new DateTime(2026, 1, 9, 5, 30, 31, 176, DateTimeKind.Utc).AddTicks(7159), "AQAAAAIAAYagAAAAECPDXxN0+tCo2g1Kv5BnAZYWUxoP5K80BZxvn8biPLjybpXl11PhD/ug1PxdTZ/PFA==", "c34bed71-9b7e-4438-9767-d0b125020461" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Nurse",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "25782c69-3001-4e14-9a67-91d45b396ec2", new DateTime(2026, 1, 9, 5, 30, 31, 127, DateTimeKind.Utc).AddTicks(1322), "AQAAAAIAAYagAAAAEE5jkpAuidPDnz/lwyZsNHjOj0fp3TGaI9BeAzEbDa7229beYGWY/bX8waWLCEhBtg==", "70486bd5-f853-4b80-902a-d3149b03133f" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Patient",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "1887617e-6bfd-4908-9ae9-ae5f7a8bd039", new DateTime(2026, 1, 9, 5, 30, 31, 387, DateTimeKind.Utc).AddTicks(7913), "AQAAAAIAAYagAAAAENDdgDXsovgj0dC14aXhhcyi7e3M3kkW4MZ+NWM2BYPMEPVqy0Jn4FteujldKBAGsw==", "3ea1fde6-d7b3-4231-ba89-50557f78839a" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Physician",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "6bf276c0-eaf4-441d-bd45-6922b548af08", new DateTime(2026, 1, 9, 5, 30, 31, 77, DateTimeKind.Utc).AddTicks(3908), "AQAAAAIAAYagAAAAEIXv8kJxlEB31Huy5/whAXsN5wCF7OEpF7tF5hAZ7byWz9kbLuf4hQknsBp+u3WZ/A==", "b1485305-e4c7-44c9-89e1-d860fe43bbc6" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Scheduler",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "df5661ab-9cb7-4e4e-947b-51d51a2bc31f", new DateTime(2026, 1, 9, 5, 30, 31, 284, DateTimeKind.Utc).AddTicks(87), "AQAAAAIAAYagAAAAEMflWiplso9dF4C3mGcADbD0EgtCpAldr5t143OaEeq2xyd1EvbB7GPlv/jvUkA5IQ==", "12c532ca-1037-4256-ae0a-d179bcc836eb" });
        }
    }
}

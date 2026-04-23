using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zebrahoof_EMR.Migrations
{
    /// <inheritdoc />
    public partial class DropDocumentPatientFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Patients_PatientId",
                table: "Documents");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Patients_PatientId",
                table: "Documents",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

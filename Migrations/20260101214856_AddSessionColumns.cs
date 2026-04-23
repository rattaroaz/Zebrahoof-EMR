using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zebrahoof_EMR.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdleTimeoutMinutes",
                table: "UserSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                table: "UserSessions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastSeenAt",
                table: "UserSessions",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "UserSessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-admin",
                column: "ConcurrencyStamp",
                value: "3b27ed8e-8f68-427a-8743-80597dc55b13");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-billing",
                column: "ConcurrencyStamp",
                value: "db8fac39-48ee-415e-b5bf-af28ae661a73");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-lab",
                column: "ConcurrencyStamp",
                value: "69e0c516-8628-4f0b-a555-fc70b6bd94e7");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-ma",
                column: "ConcurrencyStamp",
                value: "79da496f-4b50-443d-9883-067c24feeca3");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-nurse",
                column: "ConcurrencyStamp",
                value: "fd147590-1c67-40ce-a998-17c5495dfdab");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-patient",
                column: "ConcurrencyStamp",
                value: "f1127591-9642-4d19-804f-f36029890be8");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-physician",
                column: "ConcurrencyStamp",
                value: "97b2223d-a8ce-40d9-b88c-9fa05660295c");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-scheduler",
                column: "ConcurrencyStamp",
                value: "a15a001c-5089-4445-8cbf-7782e0211ac2");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Admin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "30f520df-4c46-4740-b157-12bbddf7b708", new DateTime(2026, 1, 1, 21, 48, 55, 944, DateTimeKind.Utc).AddTicks(6055), "AQAAAAIAAYagAAAAEF4TKmuiEkSv6Juqp6Te03I6wg14E9sN1ebshFfUTdwFE6EDOj/7/+C+Tb19Epx2vQ==", "75d71a3e-c851-41e7-875a-12c6b87dd1b4" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Billing",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "d8bffa5b-7407-4799-af04-02c68bf63a8b", new DateTime(2026, 1, 1, 21, 48, 56, 145, DateTimeKind.Utc).AddTicks(4879), "AQAAAAIAAYagAAAAEInbsVSkHBmR6tNTte+FbtBfSjzE34dQ9StxKHJcmDf1HUDcH9KxB6GSNxaUhJJInQ==", "e712454b-e66a-469f-8f56-9ae104036ab0" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Lab",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "05047eec-d987-48d4-8ba4-f146245b6689", new DateTime(2026, 1, 1, 21, 48, 56, 247, DateTimeKind.Utc).AddTicks(1549), "AQAAAAIAAYagAAAAEGJL1u7V9Hk8yMXF/jQVRd/Dkv5+jXvL1z5THYPC618CwvpL6RSri3EHrCtZI2RW5A==", "fff7c015-9d48-4d7b-aed5-46391f0bb228" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "MA",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f99b1561-340f-4285-bfd3-9eba5e75c26e", new DateTime(2026, 1, 1, 21, 48, 56, 95, DateTimeKind.Utc).AddTicks(1430), "AQAAAAIAAYagAAAAEA0IsmfkvCCiXudfYN8ce/ivJAVMoHZuw5+MPd+vPu/pITLOP8iRnb0bbpwbkFNzSQ==", "fb700cf6-34e1-453d-bf73-21e01be60089" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Nurse",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "747b23e8-90e4-4a7e-9c16-c28eaa8017d9", new DateTime(2026, 1, 1, 21, 48, 56, 45, DateTimeKind.Utc).AddTicks(8252), "AQAAAAIAAYagAAAAEBf19gfCS6pKNyiutYuMulYHRtG5QsDD/gbPp6u48GOS1ZOJjJ+dsgqZvBDCEdPXMQ==", "d5b98e88-4748-4a56-8ea1-419aae328411" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Patient",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "041f2fb4-e7d6-477c-8a64-4c05692bf8b5", new DateTime(2026, 1, 1, 21, 48, 56, 297, DateTimeKind.Utc).AddTicks(4155), "AQAAAAIAAYagAAAAEOl6LI3TqPwK53hfHIUKfKg/ZJl4qAtTbWUx8oLiHxLbzHZPjKnb0fSN9fMqw1mqgA==", "d55d0218-065d-46ce-b3af-1155a4ff9009" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Physician",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "02502d23-1cc5-47b8-bb79-17901a378db4", new DateTime(2026, 1, 1, 21, 48, 55, 995, DateTimeKind.Utc).AddTicks(811), "AQAAAAIAAYagAAAAECVfSu0X3rZj7SaiW27/itE4XyxT11DykvGpTDi4CJL6CK4SWG1k3A3otONX7oD+DQ==", "bd850f7d-ada1-4d8e-bd42-09c67fe1a92c" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Scheduler",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "5a02bf90-0638-4224-8357-054772d46dff", new DateTime(2026, 1, 1, 21, 48, 56, 195, DateTimeKind.Utc).AddTicks(9295), "AQAAAAIAAYagAAAAELi3PVx8f1YMZGuJkCBuJn8cVx+ORmcsePuCPfRnP+qRQsP3NHHxXPE1+FPdpW4xtg==", "6c22b23d-fc56-451e-9c61-56c52ea2b5f6" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId1",
                table: "UserSessions",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSessions_AspNetUsers_UserId1",
                table: "UserSessions",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSessions_AspNetUsers_UserId1",
                table: "UserSessions");

            migrationBuilder.DropIndex(
                name: "IX_UserSessions_UserId1",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "IdleTimeoutMinutes",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "IsRevoked",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "UserSessions");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-admin",
                column: "ConcurrencyStamp",
                value: "d12c914d-0bec-47d4-826f-fc97787e3d19");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-billing",
                column: "ConcurrencyStamp",
                value: "fecf3e9f-f94a-4f1a-9a5f-315aa8731fb8");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-lab",
                column: "ConcurrencyStamp",
                value: "d421a4e0-7dab-4bfc-80ec-2757f6f1c7bd");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-ma",
                column: "ConcurrencyStamp",
                value: "8f626ab8-2bdc-47ea-8a8d-615dcd7eaf97");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-nurse",
                column: "ConcurrencyStamp",
                value: "96a9e87a-2b5d-436b-b582-aaf7a0a4bf69");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-patient",
                column: "ConcurrencyStamp",
                value: "c0ca988d-fec0-4eda-8b87-b8f89b4d9c43");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-physician",
                column: "ConcurrencyStamp",
                value: "eaa790d8-4a99-4afd-8bf5-f8795ca3e750");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-scheduler",
                column: "ConcurrencyStamp",
                value: "3261eab5-0286-413c-9c9a-ea1ad4f24b6b");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Admin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "5e12719c-aad1-490c-bee9-9b9d3c3ae9eb", new DateTime(2026, 1, 1, 19, 27, 35, 796, DateTimeKind.Utc).AddTicks(3888), "AQAAAAIAAYagAAAAEHaG7ieu1XMPsbb7ticZndGfu7UWgYlyhUbu1iM34xre4wxnK8eC6+bIvPy194kMJQ==", "01276d82-75c4-4277-a97e-1418d7ca64dd" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Billing",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "e17a1930-cbec-404a-98e7-3c85ff604f63", new DateTime(2026, 1, 1, 19, 27, 35, 999, DateTimeKind.Utc).AddTicks(9328), "AQAAAAIAAYagAAAAEFLWpL+bxzDIVbIbRlXaja6JDvqC1MRtIwLOjW3s+MneFq/4eo2IVl0tdRrTe8+uBA==", "c7cb0732-b9a0-4bae-90c7-d038e21a2216" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Lab",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f9269baa-dfcd-41f3-9665-bd15137b1d3e", new DateTime(2026, 1, 1, 19, 27, 36, 103, DateTimeKind.Utc).AddTicks(6882), "AQAAAAIAAYagAAAAEFysa7VKxG9pf4Ua5wwIF5j4vZXUynCSGEogfcsX6l1xc9My7LQoEkoCGBBlfq2v2w==", "1251368e-7c93-415b-ae1f-d4c137bf2cbb" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "MA",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "128a7ece-b689-4c7f-8269-1cffc03343fa", new DateTime(2026, 1, 1, 19, 27, 35, 947, DateTimeKind.Utc).AddTicks(8882), "AQAAAAIAAYagAAAAEAyeoZSklz8Ow+Ivr3zdOlplX9dc6Yvj0cOELg6WkG88dlLtklP9zwVK4VAvP//B6Q==", "5fcef561-99e5-4b34-9d14-eb058cfb8bd7" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Nurse",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "51fd6800-eaca-4384-a451-b7cadee43a98", new DateTime(2026, 1, 1, 19, 27, 35, 897, DateTimeKind.Utc).AddTicks(1867), "AQAAAAIAAYagAAAAEBykJfKuEB0Cks/qtyCL7jRFUekYIVjCQsky0yWAFSdVx0JrOsDDOrdPwNCjZgqfwQ==", "4c1e648d-229e-46e0-b8e4-6e4b1b3ad100" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Patient",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "7f00d42a-ad2e-42cf-8c8d-b25bfd10e2b2", new DateTime(2026, 1, 1, 19, 27, 36, 155, DateTimeKind.Utc).AddTicks(1998), "AQAAAAIAAYagAAAAEIO7/gC2XsDWiMS6ENNbKS99Gpy91Qywo646HzNfq1Y2fdY2H2lrmSUoGi0rS47C4g==", "4932699b-e23a-4d78-887e-28a96d70d1a5" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Physician",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "77467bbe-969d-4158-a6f1-1ebb00746245", new DateTime(2026, 1, 1, 19, 27, 35, 847, DateTimeKind.Utc).AddTicks(374), "AQAAAAIAAYagAAAAEPxh8GOk/GMHeyRrs1KVR5xwdQel7J8k6+wMJINHps1ulPp31+fZJ9CR+fhYBw/IoQ==", "b9e6cbf6-963e-4af4-a255-b46cbdd13f37" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Scheduler",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "00f891c4-eb3c-4e30-ac2f-5274327313ce", new DateTime(2026, 1, 1, 19, 27, 36, 51, DateTimeKind.Utc).AddTicks(3660), "AQAAAAIAAYagAAAAEC/HdRlmyhMx8F/oZcM+0zDG0SEncns9gQ1AW69IDuqj26lwAHT1hSIvDYZjRPOUkg==", "929e923f-a152-4525-9a03-6cba23ca4d37" });
        }
    }
}

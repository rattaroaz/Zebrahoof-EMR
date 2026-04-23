using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zebrahoof_EMR.Migrations
{
    /// <inheritdoc />
    public partial class AddStickyNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UploadedBy = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Content = table.Column<byte[]>(type: "BLOB", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StickyNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    NoteNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    X = table.Column<double>(type: "REAL", nullable: false),
                    Y = table.Column<double>(type: "REAL", nullable: false),
                    IsVisible = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReminderDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StickyNotes", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PatientId",
                table: "Documents",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_StickyNotes_UserId",
                table: "StickyNotes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "StickyNotes");

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
        }
    }
}

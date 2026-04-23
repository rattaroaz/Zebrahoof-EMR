using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zebrahoof_EMR.Migrations
{
    /// <inheritdoc />
    public partial class AddPatientChartTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareTeamMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Specialty = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Fax = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Organization = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareTeamMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalAlerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: true),
                    PatientName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsAcknowledged = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EncounterId = table.Column<int>(type: "INTEGER", nullable: false),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    AuthorName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ChiefComplaint = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    HistoryOfPresentIllness = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewOfSystems = table.Column<string>(type: "TEXT", nullable: true),
                    PhysicalExam = table.Column<string>(type: "TEXT", nullable: true),
                    Assessment = table.Column<string>(type: "TEXT", nullable: true),
                    Plan = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyContacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Relationship = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Encounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    PatientName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VisitType = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ChiefComplaint = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Assessment = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Plan = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Encounters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImagingOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    EncounterId = table.Column<int>(type: "INTEGER", nullable: true),
                    Modality = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    BodyPart = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    StudyDescription = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ClinicalIndication = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    WithContrast = table.Column<bool>(type: "INTEGER", nullable: false),
                    ContrastType = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    OrderingProvider = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OrderedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagingOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImagingStudies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    StudyDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Modality = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    BodyPart = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Impression = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    OrderingProvider = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Radiologist = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagingStudies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Immunizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    VaccineName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Manufacturer = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LotNumber = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    AdministeredDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Site = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Route = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    AdministeredBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Immunizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Insurances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    PayerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PlanName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MemberId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    GroupNumber = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TerminationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Insurances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    EncounterId = table.Column<int>(type: "INTEGER", nullable: true),
                    TestName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    PanelName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ClinicalIndication = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    SpecialInstructions = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    OrderingProvider = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OrderedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsFasting = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    TestName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PanelName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Value = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Units = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    ReferenceRange = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CollectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResultedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsAbnormal = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCritical = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatientInteractions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    PatientName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Provider = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientInteractions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    MedicationId = table.Column<int>(type: "INTEGER", nullable: true),
                    MedicationName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Dose = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Route = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Frequency = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    DaysSupply = table.Column<int>(type: "INTEGER", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: true),
                    Refills = table.Column<int>(type: "INTEGER", nullable: true),
                    Instructions = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Prescriber = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PrescribedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Pharmacy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsRefill = table.Column<bool>(type: "INTEGER", nullable: false),
                    DiscontinuedReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferralOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    EncounterId = table.Column<int>(type: "INTEGER", nullable: true),
                    Specialty = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ReferToProvider = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ReferToOrganization = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ClinicalHistory = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    QuestionsForConsultant = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    OrderingProvider = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    OrderedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferralOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VitalSigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PatientId = table.Column<int>(type: "INTEGER", nullable: false),
                    EncounterId = table.Column<int>(type: "INTEGER", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecordedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Temperature = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    SystolicBP = table.Column<int>(type: "INTEGER", nullable: true),
                    DiastolicBP = table.Column<int>(type: "INTEGER", nullable: true),
                    HeartRate = table.Column<int>(type: "INTEGER", nullable: true),
                    RespiratoryRate = table.Column<int>(type: "INTEGER", nullable: true),
                    OxygenSaturation = table.Column<int>(type: "INTEGER", nullable: true),
                    Weight = table.Column<decimal>(type: "TEXT", precision: 6, scale: 2, nullable: true),
                    Height = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    BMI = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalSigns", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-admin",
                column: "ConcurrencyStamp",
                value: "74f1ff15-7060-4fcf-8a34-5587426bd023");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-billing",
                column: "ConcurrencyStamp",
                value: "91dc1e49-1ae4-4327-a2d7-8bd1df1be07c");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-lab",
                column: "ConcurrencyStamp",
                value: "88a683cf-33a8-458f-a56d-38bca28ee7d7");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-ma",
                column: "ConcurrencyStamp",
                value: "72a15314-7aba-4aa4-963c-21bc1e8ccc76");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-nurse",
                column: "ConcurrencyStamp",
                value: "6cc3c790-5ac2-4c38-9478-4a1122e8d549");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-patient",
                column: "ConcurrencyStamp",
                value: "dbcc200c-aaf5-4228-8abf-16de3118ffbb");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-physician",
                column: "ConcurrencyStamp",
                value: "e835bef1-a3a6-4b6f-9608-da7dd5ad4267");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "role-scheduler",
                column: "ConcurrencyStamp",
                value: "4307270d-6754-44e4-a40d-c38f5c4ec179");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Admin",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "ca94f3f3-e2f5-4815-9532-d6a758d834f4", new DateTime(2026, 4, 20, 0, 54, 32, 934, DateTimeKind.Utc).AddTicks(5027), "AQAAAAIAAYagAAAAECZSPxkt7uUnSTK6qTxrnhzuOivmi8Z/D2jNGwjZ86y4AAchiKAcURd88r79WYp3TQ==", "b3eba05b-23bd-4dc3-a32e-dad1bcd6e72c" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Billing",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "e4c31cf3-6226-4d75-bb35-57a589de331e", new DateTime(2026, 4, 20, 0, 54, 33, 188, DateTimeKind.Utc).AddTicks(8865), "AQAAAAIAAYagAAAAEARjFDjTNOF+U4myLkDYjAthFczjki2lecwX89R/GGZM7uHkbd++psg2WswTtSlDng==", "28730d23-259e-42e8-9001-5756d0f3c165" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Lab",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "57538f89-f961-4f27-9c2e-12e8c31e5b6e", new DateTime(2026, 4, 20, 0, 54, 33, 301, DateTimeKind.Utc).AddTicks(253), "AQAAAAIAAYagAAAAECyZ6CVnuomVQPV8ypCCzqxf8hn07M5VjFoNGCq5/0NdqO744Px3cSiX3qvHSVmdIg==", "c721545f-cf24-4e72-b598-0402c063e9bc" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "MA",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "2fe26526-12bb-4406-baed-6adbad9c6a30", new DateTime(2026, 4, 20, 0, 54, 33, 113, DateTimeKind.Utc).AddTicks(3), "AQAAAAIAAYagAAAAEGpDpTfvFKnwadpLj6ko1Defu+XWG3QXFIgUFXKM8QRN2JnXejJJYmuFsDwPOcmIXA==", "e35fc388-243f-4666-9906-fdf109b1dd91" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Nurse",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "d3b751fb-4700-446e-bce6-6d63a0041b26", new DateTime(2026, 4, 20, 0, 54, 33, 54, DateTimeKind.Utc).AddTicks(6956), "AQAAAAIAAYagAAAAEO3hiuqUB3QaAXiLyNTZYYaFFpiDdbI1U0G9dMN5Sf/mNmO8eo/5jQjfhL2GF1t0tg==", "999fc2f9-58bf-4d86-b4fe-a8f327f95083" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Patient",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "c2fa3722-e64c-426e-a583-4e850f52ac39", new DateTime(2026, 4, 20, 0, 54, 33, 354, DateTimeKind.Utc).AddTicks(1716), "AQAAAAIAAYagAAAAEMWHPskXv+UbNLUQ8anfMfL3OUK59KwL28rGUwuUO3wzwJEFy1uKCg35TFscDpTc4Q==", "11b0492c-4675-4d49-94a9-b8d610e05487" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Physician",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "8ef9704f-0d6e-4c2e-bfcd-d46c6a91ba65", new DateTime(2026, 4, 20, 0, 54, 32, 992, DateTimeKind.Utc).AddTicks(8869), "AQAAAAIAAYagAAAAEJ1FCu4wxea6FuNwo6c3r7Yyu2avtJnIFkymFxYu/pQFE1uJujicQHcI1PnAsAwEzA==", "667a2be2-b0a0-474e-85bf-ddec8097f3c2" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "Scheduler",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "0fc80055-4d0d-4657-8a16-1269dd318764", new DateTime(2026, 4, 20, 0, 54, 33, 245, DateTimeKind.Utc).AddTicks(9874), "AQAAAAIAAYagAAAAEPC9UkBN6WJJNgjPb4Rw4JjpdX7B8ufidNXUb+xGKPyy8R2GXhsBXX/zFGdLvIvgEg==", "d939e2b2-f921-43ba-b41e-868f7be89827" });

            migrationBuilder.CreateIndex(
                name: "IX_CareTeamMembers_PatientId",
                table: "CareTeamMembers",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalAlerts_PatientId_IsAcknowledged",
                table: "ClinicalAlerts",
                columns: new[] { "PatientId", "IsAcknowledged" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_EncounterId",
                table: "ClinicalNotes",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_PatientId_CreatedAt",
                table: "ClinicalNotes",
                columns: new[] { "PatientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContacts_PatientId",
                table: "EmergencyContacts",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_PatientId_DateTime",
                table: "Encounters",
                columns: new[] { "PatientId", "DateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ImagingOrders_PatientId_OrderedAt",
                table: "ImagingOrders",
                columns: new[] { "PatientId", "OrderedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ImagingStudies_PatientId_StudyDate",
                table: "ImagingStudies",
                columns: new[] { "PatientId", "StudyDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Immunizations_PatientId_AdministeredDate",
                table: "Immunizations",
                columns: new[] { "PatientId", "AdministeredDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Insurances_PatientId_IsPrimary",
                table: "Insurances",
                columns: new[] { "PatientId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_PatientId_OrderedAt",
                table: "LabOrders",
                columns: new[] { "PatientId", "OrderedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_PatientId_CollectedAt",
                table: "LabResults",
                columns: new[] { "PatientId", "CollectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientInteractions_PatientId_DateTime",
                table: "PatientInteractions",
                columns: new[] { "PatientId", "DateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_MedicationId",
                table: "Prescriptions",
                column: "MedicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId_PrescribedDate",
                table: "Prescriptions",
                columns: new[] { "PatientId", "PrescribedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ReferralOrders_PatientId_OrderedAt",
                table: "ReferralOrders",
                columns: new[] { "PatientId", "OrderedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_PatientId_RecordedAt",
                table: "VitalSigns",
                columns: new[] { "PatientId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareTeamMembers");

            migrationBuilder.DropTable(
                name: "ClinicalAlerts");

            migrationBuilder.DropTable(
                name: "ClinicalNotes");

            migrationBuilder.DropTable(
                name: "EmergencyContacts");

            migrationBuilder.DropTable(
                name: "Encounters");

            migrationBuilder.DropTable(
                name: "ImagingOrders");

            migrationBuilder.DropTable(
                name: "ImagingStudies");

            migrationBuilder.DropTable(
                name: "Immunizations");

            migrationBuilder.DropTable(
                name: "Insurances");

            migrationBuilder.DropTable(
                name: "LabOrders");

            migrationBuilder.DropTable(
                name: "LabResults");

            migrationBuilder.DropTable(
                name: "PatientInteractions");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "ReferralOrders");

            migrationBuilder.DropTable(
                name: "VitalSigns");

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
        }
    }
}

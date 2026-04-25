using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    private static readonly ValueConverter<List<string>, string> StringListConverter = new(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());

    private static readonly ValueComparer<List<string>> StringListComparer = new(
        (l, r) =>
            ReferenceEquals(l, r) ||
            (l == null && r == null) ||
            (l != null && r != null && l.SequenceEqual(r)),
        v => v == null
            ? 0
            : v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item == null ? 0 : item.GetHashCode())),
        v => v == null ? new List<string>() : new List<string>(v));

    private static readonly ValueConverter<string?, string?> EncryptedStringConverter = new(
        v => FieldEncryptionHelper.Encrypt(v),
        v => FieldEncryptionHelper.Decrypt(v));

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ClinicalTask> ClinicalTasks => Set<ClinicalTask>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<StickyNote> StickyNotes => Set<StickyNote>();
    public DbSet<PatientStickyNote> PatientStickyNotes => Set<PatientStickyNote>();
    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<Allergy> Allergies => Set<Allergy>();
    public DbSet<EncounterMessage> EncounterMessages => Set<EncounterMessage>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<VitalSigns> VitalSigns => Set<VitalSigns>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();
    public DbSet<Immunization> Immunizations => Set<Immunization>();
    public DbSet<ImagingStudy> ImagingStudies => Set<ImagingStudy>();
    public DbSet<CareTeamMember> CareTeamMembers => Set<CareTeamMember>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
    public DbSet<Insurance> Insurances => Set<Insurance>();
    public DbSet<PatientInteraction> PatientInteractions => Set<PatientInteraction>();
    public DbSet<ClinicalAlert> ClinicalAlerts => Set<ClinicalAlert>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<LabOrder> LabOrders => Set<LabOrder>();
    public DbSet<ImagingOrder> ImagingOrders => Set<ImagingOrder>();
    public DbSet<ReferralOrder> ReferralOrders => Set<ReferralOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurePatients(modelBuilder);
        ConfigureAppointments(modelBuilder);
        ConfigureClinicalTasks(modelBuilder);
        ConfigureMessages(modelBuilder);
        ConfigureIdentity(modelBuilder);
        ConfigureUserProfiles(modelBuilder);
        ConfigureUserSessions(modelBuilder);
        ConfigureAuditLogs(modelBuilder);
        ConfigureDocuments(modelBuilder);
        ConfigureStickyNotes(modelBuilder);
        ConfigurePatientStickyNotes(modelBuilder);
        ConfigureProblems(modelBuilder);
        ConfigureMedications(modelBuilder);
        ConfigureAllergies(modelBuilder);
        ConfigureEncounterMessages(modelBuilder);
        ConfigurePatientChartEntities(modelBuilder);
        SeedIdentityData(modelBuilder);
    }

    private static void ConfigurePatientStickyNotes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PatientStickyNote>(entity =>
        {
            entity.HasIndex(p => new { p.UserId, p.PatientId }).IsUnique();
            entity.Property(p => p.UserId).HasMaxLength(256);
            entity.Property(p => p.Content).HasMaxLength(4000);
            entity.Property(p => p.Color).HasConversion<string>();
        });
    }

    private static void ConfigureStickyNotes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StickyNote>(entity =>
        {
            entity.HasIndex(s => s.UserId);
            entity.Property(s => s.UserId).HasMaxLength(256);
            entity.Property(s => s.Content).HasMaxLength(4000);
            entity.Property(s => s.Color).HasConversion<string>();
        });
    }

    private static void ConfigurePatients(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasIndex(p => p.MRN).IsUnique();
            entity.Property(p => p.FirstName).HasMaxLength(100);
            entity.Property(p => p.LastName).HasMaxLength(100);
            entity.Property(p => p.Email).HasMaxLength(256);
            entity.Property(p => p.Phone).HasMaxLength(32);
            entity.Property(p => p.Allergies)
                  .HasConversion(StringListConverter)
                  .Metadata.SetValueComparer(StringListComparer);
            entity.Property(p => p.Alerts)
                  .HasConversion(StringListConverter)
                  .Metadata.SetValueComparer(StringListComparer);
        });
    }

    private static void ConfigureAppointments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasIndex(a => new { a.PatientId, a.DateTime });
            entity.Property(a => a.Provider).HasMaxLength(200);
            entity.Property(a => a.Location).HasMaxLength(200);
            entity.Property(a => a.VisitType).HasMaxLength(200);
        });
    }

    private static void ConfigureClinicalTasks(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClinicalTask>(entity =>
        {
            entity.HasIndex(t => new { t.AssignedTo, t.Status });
            entity.Property(t => t.AssignedTo).HasMaxLength(200);
            entity.Property(t => t.CreatedBy).HasMaxLength(200);
        });
    }

    private static void ConfigureMessages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasIndex(m => m.To);
            entity.Property(m => m.Subject).HasMaxLength(200);
            entity.Property(m => m.From).HasMaxLength(200);
            entity.Property(m => m.To).HasMaxLength(200);
        });
    }

    private void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.DisplayName).HasMaxLength(256);
            entity.Property(u => u.Department).HasMaxLength(128);
            entity.Property(u => u.MfaSecret).HasConversion(EncryptedStringConverter);
            entity.Property(u => u.PasswordSalt).HasConversion(EncryptedStringConverter);
        });
    }

    private void ConfigureUserProfiles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(up => up.UserId);
            entity.Property(up => up.DisplayName).HasMaxLength(256);
            entity.Property(up => up.Department).HasMaxLength(128);
            entity.HasOne<ApplicationUser>()
                  .WithOne()
                  .HasForeignKey<UserProfile>(up => up.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureUserSessions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.Property(us => us.RefreshToken)
                  .HasConversion(EncryptedStringConverter);
            entity.Property(us => us.DeviceFingerprint)
                  .HasMaxLength(256);
            entity.HasOne(us => us.User)
                  .WithMany()
                  .HasForeignKey(us => us.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(us => new { us.UserId, us.ExpiresAt });
        });
    }

    private void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.Property(al => al.Action).HasMaxLength(128);
            entity.Property(al => al.Scope).HasMaxLength(256);
            entity.Property(al => al.Metadata)
                  .HasConversion(EncryptedStringConverter);
            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(al => al.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(al => new { al.UserId, al.Timestamp });
        });
    }

    private void ConfigureDocuments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            // Patients currently live only in MockPatientService (in-memory) and are
            // not persisted in the Patients table, so a real FK constraint cannot be
            // satisfied. We keep PatientId as a plain indexed column and ignore the
            // navigation property until patients are persisted.
            entity.Ignore(d => d.Patient);
            entity.HasIndex(d => d.PatientId);
            entity.Property(d => d.Name).HasMaxLength(256);
            entity.Property(d => d.Category).HasMaxLength(128);
            entity.Property(d => d.Type).HasMaxLength(50);
            entity.Property(d => d.UploadedBy).HasMaxLength(256);
            entity.Property(d => d.FileName).HasMaxLength(256);
        });
    }

    private static void ConfigureProblems(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Problem>(entity =>
        {
            entity.HasIndex(p => p.PatientId);
            entity.Property(p => p.Name).HasMaxLength(256);
            entity.Property(p => p.IcdCode).HasMaxLength(32);
            entity.Property(p => p.Severity).HasMaxLength(64);
            entity.Property(p => p.Notes).HasMaxLength(2000);
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(32);
        });
    }

    private static void ConfigureMedications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Medication>(entity =>
        {
            entity.HasIndex(m => m.PatientId);
            entity.Property(m => m.Name).HasMaxLength(256);
            entity.Property(m => m.Dose).HasMaxLength(128);
            entity.Property(m => m.Route).HasMaxLength(64);
            entity.Property(m => m.Frequency).HasMaxLength(128);
            entity.Property(m => m.Prescriber).HasMaxLength(256);
            entity.Property(m => m.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(m => m.Instructions).HasMaxLength(1000);
            entity.Property(m => m.Pharmacy).HasMaxLength(256);
        });
    }

    private static void ConfigureEncounterMessages(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EncounterMessage>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => new { m.PatientId, m.CreatedAt });
            entity.Property(m => m.UserId).HasMaxLength(256).IsRequired();
            entity.Property(m => m.UserInput).IsRequired();
            entity.Property(m => m.GrokResponse).IsRequired();
        });
    }

    /// <summary>
    /// Persistent patient-chart tables. Patients themselves currently live in
    /// MockPatientService (in-memory) so a real FK from these rows back to a
    /// Patients table is intentionally omitted; we keep PatientId as a plain
    /// indexed integer.
    /// </summary>
    private static void ConfigurePatientChartEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Encounter>(entity =>
        {
            entity.HasIndex(e => new { e.PatientId, e.DateTime });
            entity.Property(e => e.PatientName).HasMaxLength(200);
            entity.Property(e => e.VisitType).HasMaxLength(200);
            entity.Property(e => e.Provider).HasMaxLength(200);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(e => e.ChiefComplaint).HasMaxLength(1000);
            entity.Property(e => e.Assessment).HasMaxLength(4000);
            entity.Property(e => e.Plan).HasMaxLength(4000);
        });

        modelBuilder.Entity<VitalSigns>(entity =>
        {
            entity.HasIndex(v => new { v.PatientId, v.RecordedAt });
            entity.Property(v => v.RecordedBy).HasMaxLength(200);
            entity.Property(v => v.Temperature).HasPrecision(5, 2);
            entity.Property(v => v.Weight).HasPrecision(6, 2);
            entity.Property(v => v.Height).HasPrecision(5, 2);
            entity.Property(v => v.BMI).HasPrecision(5, 2);
        });

        modelBuilder.Entity<LabResult>(entity =>
        {
            entity.HasIndex(l => new { l.PatientId, l.CollectedAt });
            entity.Property(l => l.TestName).HasMaxLength(200).IsRequired();
            entity.Property(l => l.PanelName).HasMaxLength(200);
            entity.Property(l => l.Value).HasMaxLength(200);
            entity.Property(l => l.Units).HasMaxLength(64);
            entity.Property(l => l.ReferenceRange).HasMaxLength(128);
            entity.Property(l => l.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<ClinicalNote>(entity =>
        {
            entity.HasIndex(n => new { n.PatientId, n.CreatedAt });
            entity.HasIndex(n => n.EncounterId);
            entity.Property(n => n.AuthorName).HasMaxLength(256);
            entity.Property(n => n.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(n => n.ChiefComplaint).HasMaxLength(2000);
            entity.Property(n => n.HistoryOfPresentIllness);
            entity.Property(n => n.ReviewOfSystems);
            entity.Property(n => n.PhysicalExam);
            entity.Property(n => n.Assessment);
            entity.Property(n => n.Plan);
        });

        modelBuilder.Entity<Immunization>(entity =>
        {
            entity.HasIndex(i => new { i.PatientId, i.AdministeredDate });
            entity.Property(i => i.VaccineName).HasMaxLength(256).IsRequired();
            entity.Property(i => i.Manufacturer).HasMaxLength(200);
            entity.Property(i => i.LotNumber).HasMaxLength(64);
            entity.Property(i => i.Site).HasMaxLength(64);
            entity.Property(i => i.Route).HasMaxLength(64);
            entity.Property(i => i.AdministeredBy).HasMaxLength(200);
            entity.Property(i => i.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(i => i.Notes).HasMaxLength(2000);
        });

        modelBuilder.Entity<ImagingStudy>(entity =>
        {
            entity.HasIndex(i => new { i.PatientId, i.StudyDate });
            entity.Property(i => i.Modality).HasMaxLength(64);
            entity.Property(i => i.BodyPart).HasMaxLength(128);
            entity.Property(i => i.Description).HasMaxLength(512);
            entity.Property(i => i.Impression).HasMaxLength(4000);
            entity.Property(i => i.OrderingProvider).HasMaxLength(200);
            entity.Property(i => i.Radiologist).HasMaxLength(200);
            entity.Property(i => i.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<CareTeamMember>(entity =>
        {
            entity.HasIndex(c => c.PatientId);
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Role).HasMaxLength(128);
            entity.Property(c => c.Specialty).HasMaxLength(128);
            entity.Property(c => c.Phone).HasMaxLength(64);
            entity.Property(c => c.Fax).HasMaxLength(64);
            entity.Property(c => c.Organization).HasMaxLength(200);
        });

        modelBuilder.Entity<EmergencyContact>(entity =>
        {
            entity.HasIndex(c => c.PatientId);
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Relationship).HasMaxLength(64);
            entity.Property(c => c.Phone).HasMaxLength(64);
            entity.Property(c => c.Email).HasMaxLength(256);
        });

        modelBuilder.Entity<Insurance>(entity =>
        {
            entity.HasIndex(i => new { i.PatientId, i.IsPrimary });
            entity.Property(i => i.PayerName).HasMaxLength(200).IsRequired();
            entity.Property(i => i.PlanName).HasMaxLength(200);
            entity.Property(i => i.MemberId).HasMaxLength(128);
            entity.Property(i => i.GroupNumber).HasMaxLength(128);
            entity.Property(i => i.Type).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<PatientInteraction>(entity =>
        {
            entity.HasIndex(i => new { i.PatientId, i.DateTime });
            entity.Property(i => i.PatientName).HasMaxLength(200);
            entity.Property(i => i.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(i => i.Summary).HasMaxLength(2000);
            entity.Property(i => i.Provider).HasMaxLength(200);
        });

        modelBuilder.Entity<ClinicalAlert>(entity =>
        {
            entity.HasIndex(a => new { a.PatientId, a.IsAcknowledged });
            entity.Property(a => a.PatientName).HasMaxLength(200);
            entity.Property(a => a.Title).HasMaxLength(256).IsRequired();
            entity.Property(a => a.Description).HasMaxLength(2000);
            entity.Property(a => a.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(a => a.Severity).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasIndex(p => new { p.PatientId, p.PrescribedDate });
            entity.HasIndex(p => p.MedicationId);
            entity.Property(p => p.MedicationName).HasMaxLength(256).IsRequired();
            entity.Property(p => p.Dose).HasMaxLength(128);
            entity.Property(p => p.Route).HasMaxLength(64);
            entity.Property(p => p.Frequency).HasMaxLength(128);
            entity.Property(p => p.Instructions).HasMaxLength(1000);
            entity.Property(p => p.Prescriber).HasMaxLength(200);
            entity.Property(p => p.Pharmacy).HasMaxLength(200);
            entity.Property(p => p.DiscontinuedReason).HasMaxLength(500);
            entity.Property(p => p.Type).HasConversion<string>().HasMaxLength(32);
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<LabOrder>(entity =>
        {
            entity.HasIndex(o => new { o.PatientId, o.OrderedAt });
            entity.Property(o => o.TestName).HasMaxLength(256).IsRequired();
            entity.Property(o => o.PanelName).HasMaxLength(200);
            entity.Property(o => o.Priority).HasMaxLength(32);
            entity.Property(o => o.ClinicalIndication).HasMaxLength(1000);
            entity.Property(o => o.SpecialInstructions).HasMaxLength(1000);
            entity.Property(o => o.OrderingProvider).HasMaxLength(200);
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<ImagingOrder>(entity =>
        {
            entity.HasIndex(o => new { o.PatientId, o.OrderedAt });
            entity.Property(o => o.Modality).HasMaxLength(64);
            entity.Property(o => o.BodyPart).HasMaxLength(128);
            entity.Property(o => o.StudyDescription).HasMaxLength(512);
            entity.Property(o => o.Priority).HasMaxLength(32);
            entity.Property(o => o.ClinicalIndication).HasMaxLength(1000);
            entity.Property(o => o.ContrastType).HasMaxLength(128);
            entity.Property(o => o.OrderingProvider).HasMaxLength(200);
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<ReferralOrder>(entity =>
        {
            entity.HasIndex(o => new { o.PatientId, o.OrderedAt });
            entity.Property(o => o.Specialty).HasMaxLength(200);
            entity.Property(o => o.ReferToProvider).HasMaxLength(200);
            entity.Property(o => o.ReferToOrganization).HasMaxLength(200);
            entity.Property(o => o.Reason).HasMaxLength(2000);
            entity.Property(o => o.Priority).HasMaxLength(32);
            entity.Property(o => o.ClinicalHistory).HasMaxLength(4000);
            entity.Property(o => o.QuestionsForConsultant).HasMaxLength(2000);
            entity.Property(o => o.OrderingProvider).HasMaxLength(200);
            entity.Property(o => o.Status).HasConversion<string>().HasMaxLength(32);
        });
    }

    private static void ConfigureAllergies(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Allergy>(entity =>
        {
            entity.HasIndex(a => a.PatientId);
            entity.Property(a => a.Allergen).HasMaxLength(256);
            entity.Property(a => a.Reaction).HasMaxLength(512);
            entity.Property(a => a.Severity).HasConversion<string>().HasMaxLength(32);
            entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(32);
        });
    }

    private void SeedIdentityData(ModelBuilder modelBuilder)
    {
        var roles = new[]
        {
            SeedRole("Admin"),
            SeedRole("Physician"),
            SeedRole("Nurse"),
            SeedRole("MA"),
            SeedRole("Billing"),
            SeedRole("Scheduler"),
            SeedRole("Lab"),
            SeedRole("Patient")
        };

        modelBuilder.Entity<IdentityRole>().HasData(roles);

        var passwordHasher = new PasswordHasher<ApplicationUser>();
        var users = new List<ApplicationUser>();
        var userProfiles = new List<UserProfile>();
        var userRoles = new List<IdentityUserRole<string>>();

        void AddUser(string id, string email, string displayName, string department, string roleName)
        {
            var user = new ApplicationUser
            {
                Id = id,
                UserName = id,
                NormalizedUserName = id.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                DisplayName = displayName,
                Department = department,
                IsActive = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                MfaSecret = null,
                PasswordSalt = null
            };

            user.PasswordHash = passwordHasher.HashPassword(user, "password");
            users.Add(user);

            userProfiles.Add(new UserProfile
            {
                UserId = id,
                DisplayName = displayName,
                Department = department
            });

            userRoles.Add(new IdentityUserRole<string>
            {
                UserId = id,
                RoleId = roles.Single(r => r.Name == roleName).Id
            });
        }

        AddUser("Admin", "admin@zebrahoof.com", "System Administrator", "Administration", "Admin");
        AddUser("Physician", "physician@zebrahoof.com", "Attending Physician", "Clinical", "Physician");
        AddUser("Nurse", "nurse@zebrahoof.com", "Charge Nurse", "Clinical", "Nurse");
        AddUser("MA", "ma@zebrahoof.com", "Medical Assistant", "Clinical Support", "MA");
        AddUser("Billing", "billing@zebrahoof.com", "Billing Specialist", "Revenue Cycle", "Billing");
        AddUser("Scheduler", "scheduler@zebrahoof.com", "Scheduling Coordinator", "Operations", "Scheduler");
        AddUser("Lab", "lab@zebrahoof.com", "Lab Technician", "Diagnostics", "Lab");
        AddUser("Patient", "patient@zebrahoof.com", "Sample Patient", "Patient Portal", "Patient");

        modelBuilder.Entity<ApplicationUser>().HasData(users);
        modelBuilder.Entity<UserProfile>().HasData(userProfiles);
        modelBuilder.Entity<IdentityUserRole<string>>().HasData(userRoles);
    }

    private static IdentityRole SeedRole(string roleName)
    {
        return new IdentityRole
        {
            Id = $"role-{roleName.ToLowerInvariant()}",
            Name = roleName,
            NormalizedName = roleName.ToUpperInvariant(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
    }
}

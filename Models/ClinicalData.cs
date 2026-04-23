namespace Zebrahoof_EMR.Models;

public class Encounter
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public string VisitType { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public EncounterStatus Status { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? Assessment { get; set; }
    public string? Plan { get; set; }
}

public enum EncounterStatus
{
    InProgress,
    Signed,
    Addended,
    Cosigned
}

public class Problem
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? IcdCode { get; set; }
    public DateTime OnsetDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public ProblemStatus Status { get; set; }
    public string? Severity { get; set; }
    public string? Notes { get; set; }
}

public enum ProblemStatus
{
    Active,
    Resolved,
    Inactive
}

public class Medication
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Dose { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Prescriber { get; set; }
    public MedicationStatus Status { get; set; }
    public bool IsHighRisk { get; set; }
    public bool IsLongTerm { get; set; } = true; // Long-term medications go on the medication list
    public string? Instructions { get; set; }
    public int? RefillsRemaining { get; set; }
    public int? DaysSupply { get; set; }
    public string? Pharmacy { get; set; }
}

public class MedicationCatalog
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> BrandNames { get; set; } = new();
    public List<string> CommonDoses { get; set; } = new();
    public List<string> CommonFrequencies { get; set; } = new();
    public List<string> CommonRoutes { get; set; } = new();
    public bool IsHighRisk { get; set; }
}

public class Prescription
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? MedicationId { get; set; } // Links to medication list if long-term
    public string MedicationName { get; set; } = string.Empty;
    public string Dose { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public string Frequency { get; set; } = string.Empty;
    public int? DaysSupply { get; set; }
    public int? Quantity { get; set; }
    public int? Refills { get; set; }
    public string? Instructions { get; set; }
    public string? Prescriber { get; set; }
    public DateTime PrescribedDate { get; set; }
    public PrescriptionType Type { get; set; }
    public PrescriptionStatus Status { get; set; }
    public string? Pharmacy { get; set; }
    public bool IsRefill { get; set; }
    public string? DiscontinuedReason { get; set; }
}

public enum PrescriptionType
{
    ShortTerm,
    LongTerm
}

public enum PrescriptionStatus
{
    Active,
    Filled,
    Cancelled,
    Expired
}

public enum MedicationStatus
{
    Active,
    Discontinued,
    OnHold,
    Completed // For short-term medications that have finished
}

public class Allergy
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string Allergen { get; set; } = string.Empty;
    public string? Reaction { get; set; }
    public AllergySeverity Severity { get; set; }
    public AllergyStatus Status { get; set; }
    public DateTime? OnsetDate { get; set; }
}

public enum AllergySeverity
{
    Mild,
    Moderate,
    Severe,
    LifeThreatening
}

public enum AllergyStatus
{
    Active,
    Inactive,
    Resolved
}

public class VitalSigns
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? EncounterId { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? RecordedBy { get; set; }
    public decimal? Temperature { get; set; }
    public int? SystolicBP { get; set; }
    public int? DiastolicBP { get; set; }
    public int? HeartRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? OxygenSaturation { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public decimal? BMI { get; set; }
}

public class LabResult
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string? PanelName { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Units { get; set; }
    public string? ReferenceRange { get; set; }
    public LabResultStatus Status { get; set; }
    public DateTime CollectedAt { get; set; }
    public DateTime? ResultedAt { get; set; }
    public bool IsAbnormal { get; set; }
    public bool IsCritical { get; set; }
}

public enum LabResultStatus
{
    Pending,
    Preliminary,
    Final,
    Corrected
}

public class ClinicalAlert
{
    public int Id { get; set; }
    public int? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public bool IsAcknowledged { get; set; }
}

public enum AlertType
{
    CriticalResult,
    DrugInteraction,
    AllergyWarning,
    OverdueCare,
    MedicationRenewal,
    AppointmentReminder,
    DocumentReview
}

public enum AlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public class PatientInteraction
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public InteractionType Type { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? Provider { get; set; }
}

public enum InteractionType
{
    OfficeVisit,
    PhoneCall,
    Message,
    LabReview,
    Prescription,
    Referral
}

public class EmergencyContact
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsPrimary { get; set; }
}

public class Insurance
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PayerName { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string MemberId { get; set; } = string.Empty;
    public string? GroupNumber { get; set; }
    public InsuranceType Type { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public DateTime? TerminationDate { get; set; }
    public bool IsPrimary { get; set; }
}

public enum InsuranceType
{
    Commercial,
    Medicare,
    Medicaid,
    SelfPay,
    WorkersComp,
    Other
}

public class ImagingStudy
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public DateTime StudyDate { get; set; }
    public string Modality { get; set; } = string.Empty;
    public string BodyPart { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Impression { get; set; }
    public string OrderingProvider { get; set; } = string.Empty;
    public string? Radiologist { get; set; }
    public ImagingStatus Status { get; set; }
}

public enum ImagingStatus
{
    Ordered,
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}

public class Immunization
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string VaccineName { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? LotNumber { get; set; }
    public DateTime AdministeredDate { get; set; }
    public string? Site { get; set; }
    public string? Route { get; set; }
    public string AdministeredBy { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public ImmunizationStatus Status { get; set; }
    public string? Notes { get; set; }
}

public enum ImmunizationStatus
{
    Completed,
    Refused,
    NotGiven,
    Historical
}

public class CareTeamMember
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Organization { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime? StartDate { get; set; }
}

public class ClinicalNote
{
    public int Id { get; set; }
    public int EncounterId { get; set; }
    public int PatientId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SignedAt { get; set; }
    public NoteStatus Status { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? HistoryOfPresentIllness { get; set; }
    public string? ReviewOfSystems { get; set; }
    public string? PhysicalExam { get; set; }
    public string? Assessment { get; set; }
    public string? Plan { get; set; }
}

public enum NoteStatus
{
    InProgress,
    Signed,
    Addended,
    Cosigned
}

public class SmartPhrase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? OwnerId { get; set; }
    public bool IsShared { get; set; }
}

public class RosChecklistSection
{
    public string Name { get; set; } = string.Empty;
    public List<RosChecklistItem> Items { get; set; } = new();
}

public class RosChecklistItem
{
    public string Label { get; set; } = string.Empty;
    public bool IsPositive { get; set; }
    public string? Notes { get; set; }
}

public class PhysicalExamSection
{
    public string Name { get; set; } = string.Empty;
    public bool IsNormal { get; set; } = true;
    public List<PhysicalExamFinding> Findings { get; set; } = new();
    public string? Notes { get; set; }
}

public class PhysicalExamFinding
{
    public string Label { get; set; } = string.Empty;
    public bool IsNormal { get; set; } = true;
    public string? Details { get; set; }
}

public class RiskScoreEntry
{
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Scale { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.Now;
}

// Order Entry Models
public class LabOrder
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? EncounterId { get; set; }
    public string TestName { get; set; } = string.Empty;
    public string? PanelName { get; set; }
    public string Priority { get; set; } = "Routine";
    public string? ClinicalIndication { get; set; }
    public string? SpecialInstructions { get; set; }
    public string OrderingProvider { get; set; } = string.Empty;
    public DateTime OrderedAt { get; set; }
    public OrderStatus Status { get; set; }
    public bool IsFasting { get; set; }
}

public class LabPanel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> IncludedTests { get; set; } = new();
    public bool RequiresFasting { get; set; }
}

public class ImagingOrder
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? EncounterId { get; set; }
    public string Modality { get; set; } = string.Empty;
    public string BodyPart { get; set; } = string.Empty;
    public string StudyDescription { get; set; } = string.Empty;
    public string Priority { get; set; } = "Routine";
    public string? ClinicalIndication { get; set; }
    public bool WithContrast { get; set; }
    public string? ContrastType { get; set; }
    public string OrderingProvider { get; set; } = string.Empty;
    public DateTime OrderedAt { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public OrderStatus Status { get; set; }
}

public class ImagingCatalog
{
    public int Id { get; set; }
    public string Modality { get; set; } = string.Empty;
    public string BodyPart { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool CanHaveContrast { get; set; }
}

public class ReferralOrder
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? EncounterId { get; set; }
    public string Specialty { get; set; } = string.Empty;
    public string? ReferToProvider { get; set; }
    public string? ReferToOrganization { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Priority { get; set; } = "Routine";
    public string? ClinicalHistory { get; set; }
    public string? QuestionsForConsultant { get; set; }
    public string OrderingProvider { get; set; } = string.Empty;
    public DateTime OrderedAt { get; set; }
    public OrderStatus Status { get; set; }
}

public enum OrderStatus
{
    Pending,
    Ordered,
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}

public class OrderCartItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public OrderType Type { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string Priority { get; set; } = "Routine";
    public object? OrderData { get; set; }
    public List<OrderWarning> Warnings { get; set; } = new();
}

public enum OrderType
{
    Medication,
    Lab,
    Imaging,
    Referral
}

public class OrderWarning
{
    public string Message { get; set; } = string.Empty;
    public OrderWarningSeverity Severity { get; set; }
    public string? Details { get; set; }
}

public enum OrderWarningSeverity
{
    Info,
    Warning,
    Critical
}

// Inbox and Messaging Models
public class InboxMessage
{
    public int Id { get; set; }
    public int? PatientId { get; set; }
    public string? PatientName { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string? FromRole { get; set; }
    public string ToName { get; set; } = string.Empty;
    public MessageCategory Category { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsRead { get; set; }
    public bool IsFlagged { get; set; }
    public bool IsUrgent { get; set; }
    public int? ParentMessageId { get; set; }
    public MessageStatus Status { get; set; }
}

public enum MessageCategory
{
    PatientMessage,
    LabResult,
    ImagingResult,
    RefillRequest,
    ReferralResponse,
    Administrative,
    ClinicalQuestion,
    TeamMessage
}

public enum MessageStatus
{
    New,
    Read,
    ActionRequired,
    Completed,
    Archived
}

// Administration Models
public class SystemUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public string? Specialty { get; set; }
    public string? NPI { get; set; }
    public int? DefaultLocationId { get; set; }
    public string? DefaultLocationName { get; set; }
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}


public enum UserStatus
{
    Active,
    Inactive,
    Locked,
    Pending
}

public class Location
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public bool IsActive { get; set; } = true;
    public LocationType Type { get; set; }
}

public enum LocationType
{
    Clinic,
    Hospital,
    UrgentCare,
    Telehealth,
    Laboratory,
    ImagingCenter
}

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? LocationId { get; set; }
    public string? LocationName { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; } = true;
}

public class NoteTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? VisitType { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsShared { get; set; }
    public bool IsActive { get; set; } = true;
}

public class OrderSet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<OrderSetItem> Items { get; set; } = new();
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsShared { get; set; }
    public bool IsActive { get; set; } = true;
}

public class OrderSetItem
{
    public OrderType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Details { get; set; }
}

public class SystemSettings
{
    public string DateFormat { get; set; } = "MM/dd/yyyy";
    public string TimeFormat { get; set; } = "h:mm tt";
    public string DefaultLandingPage { get; set; } = "/dashboard";
    public int SessionTimeoutMinutes { get; set; } = 30;
    public bool RequireTwoFactor { get; set; }
    public string OrganizationName { get; set; } = "Zebrahoof Medical Group";
    public string? OrganizationLogo { get; set; }
    public string DefaultTimezone { get; set; } = "America/New_York";
    public bool EnablePatientPortal { get; set; } = true;
    public bool EnableElectronicPrescribing { get; set; } = true;
}


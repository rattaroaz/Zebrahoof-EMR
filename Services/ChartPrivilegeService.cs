using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public enum ChartCapability
{
    ClinicalChart,
    Prescribe,
    Order,
    DocumentNotes,
    Demographics,
    Billing,
    Schedule,
    Coordinate
}

public sealed record ChartPrivilegeDecision(bool Allowed, string Message)
{
    public static ChartPrivilegeDecision Ok() => new(true, string.Empty);

    public static ChartPrivilegeDecision Deny(string message) => new(false, message);
}

/// <summary>
/// Ask AI may only make the same chart changes the signed-in role is allowed to make.
/// </summary>
public sealed class ChartPrivilegeService
{
    public static readonly IReadOnlyList<string> KnownOperations =
    [
        "add_problem", "update_problem", "resolve_problem", "remove_problem",
        "add_medication", "update_medication", "discontinue_medication",
        "prescribe", "refill",
        "add_allergy", "update_allergy", "remove_allergy", "resolve_allergy",
        "add_vitals", "add_immunization",
        "add_note", "update_note", "delete_note",
        "order", "order_lab", "order_imaging", "add_referral",
        "add_care_team",
        "create_task", "send_message", "acknowledge_alert",
        "update_demographics", "add_emergency_contact", "update_insurance",
        "update_encounter",
        "add_appointment", "schedule", "schedule_appointment",
        "cancel_appointment", "reschedule_appointment"
    ];

    public ChartPrivilegeDecision Evaluate(UserRole? role, string? op)
    {
        var normalized = Normalize(op);
        if (string.IsNullOrEmpty(normalized))
        {
            return ChartPrivilegeDecision.Deny("No chart change was specified.");
        }

        if (role is null)
        {
            return ChartPrivilegeDecision.Deny("You need to be signed in to change the chart.");
        }

        if (role == UserRole.Patient)
        {
            return ChartPrivilegeDecision.Deny("The Patient role cannot change the chart.");
        }

        if (!TryGetCapability(normalized, out var capability))
        {
            return ChartPrivilegeDecision.Deny($"'{op}' is not a supported chart change.");
        }

        if (Allows(role.Value, capability))
        {
            return ChartPrivilegeDecision.Ok();
        }

        return ChartPrivilegeDecision.Deny(
            $"Your {DisplayRole(role.Value)} role cannot {Describe(normalized)}. {WhoCan(capability)}");
    }

    public static bool TryGetCapability(string op, out ChartCapability capability)
    {
        capability = Normalize(op) switch
        {
            "add_problem" or "update_problem" or "resolve_problem" or "remove_problem"
                or "add_medication" or "update_medication" or "discontinue_medication"
                or "add_allergy" or "update_allergy" or "remove_allergy" or "resolve_allergy"
                or "add_vitals" or "add_immunization" or "add_care_team" or "update_encounter"
                => ChartCapability.ClinicalChart,
            "prescribe" or "refill" => ChartCapability.Prescribe,
            "order" or "order_lab" or "order_imaging" or "add_referral" => ChartCapability.Order,
            "add_note" or "update_note" or "delete_note" => ChartCapability.DocumentNotes,
            "update_demographics" or "add_emergency_contact" => ChartCapability.Demographics,
            "update_insurance" => ChartCapability.Billing,
            "add_appointment" or "schedule" or "schedule_appointment"
                or "cancel_appointment" or "reschedule_appointment"
                => ChartCapability.Schedule,
            "create_task" or "send_message" or "acknowledge_alert" => ChartCapability.Coordinate,
            _ => (ChartCapability)(-1)
        };

        return (int)capability >= 0;
    }

    public static bool Allows(UserRole role, ChartCapability capability) =>
        role == UserRole.Admin
        || role switch
        {
            UserRole.Physician => true,
            UserRole.Nurse => capability is not ChartCapability.Prescribe and not ChartCapability.Billing,
            UserRole.MedicalAssistant => capability is ChartCapability.ClinicalChart
                or ChartCapability.Order
                or ChartCapability.DocumentNotes
                or ChartCapability.Demographics
                or ChartCapability.Schedule
                or ChartCapability.Coordinate,
            UserRole.LabTechnician => capability is ChartCapability.Order or ChartCapability.Coordinate,
            UserRole.FrontDesk => capability is ChartCapability.Demographics
                or ChartCapability.Schedule
                or ChartCapability.Coordinate,
            UserRole.Billing => capability is ChartCapability.Billing or ChartCapability.Demographics,
            _ => false
        };

    public static string Normalize(string? op) =>
        (op ?? string.Empty).Trim().ToLowerInvariant().Replace('-', '_');

    private static string DisplayRole(UserRole role) => role switch
    {
        UserRole.MedicalAssistant => "Medical Assistant",
        UserRole.LabTechnician => "Lab Technician",
        UserRole.FrontDesk => "Front Desk",
        _ => role.ToString()
    };

    private static string Describe(string op) => op.Replace('_', ' ');

    private static string WhoCan(ChartCapability capability) => capability switch
    {
        ChartCapability.Prescribe => "A physician or administrator can do that.",
        ChartCapability.Billing => "A billing user or administrator can do that.",
        ChartCapability.ClinicalChart => "A physician, nurse, medical assistant, or administrator can do that.",
        ChartCapability.Order => "A physician, nurse, medical assistant, lab technician, or administrator can do that.",
        ChartCapability.DocumentNotes => "A physician, nurse, medical assistant, or administrator can do that.",
        ChartCapability.Demographics => "Front desk, clinical, billing, or administrator users can do that.",
        ChartCapability.Schedule => "A physician, nurse, medical assistant, front desk, or administrator can do that.",
        ChartCapability.Coordinate => "Clinical, front desk, lab, or administrator users can do that.",
        _ => "A user with the right role can do that."
    };
}

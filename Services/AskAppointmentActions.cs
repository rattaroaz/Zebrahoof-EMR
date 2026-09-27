using System.Globalization;
using System.Text.RegularExpressions;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

/// <summary>
/// Turns Ask AI schedule requests into real appointments.
/// </summary>
public sealed class AskAppointmentActions
{
    private static readonly Regex LookupPrefix = new(
        @"\b(who|what|when|where|which|how many|show|list|see)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly MockAppointmentService _appointments;

    public AskAppointmentActions(MockAppointmentService appointments)
    {
        _appointments = appointments;
    }

    public static bool LooksLikeScheduleRequest(string? question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return false;
        }

        var text = question.ToLowerInvariant();
        var mentionsCalendar = text.Contains("schedule", StringComparison.Ordinal)
                               || text.Contains("appointment", StringComparison.Ordinal)
                               || text.Contains("calendar", StringComparison.Ordinal)
                               || text.Contains("book ", StringComparison.Ordinal);
        var isCommand = text.Contains("add", StringComparison.Ordinal)
                        || text.Contains("book", StringComparison.Ordinal)
                        || text.Contains("put", StringComparison.Ordinal)
                        || text.Contains("make", StringComparison.Ordinal)
                        || text.Contains("schedule ", StringComparison.Ordinal)
                        || text.StartsWith("schedule", StringComparison.Ordinal);
        return mentionsCalendar && isCommand && !LookupPrefix.IsMatch(text);
    }

    public async Task<string> ScheduleAsync(
        ChartAskAction action,
        IReadOnlyList<Patient> roster,
        int? defaultPatientId = null)
    {
        var patient = ResolvePatient(action, roster, defaultPatientId);
        if (patient == null)
        {
            return "skip: schedule needs a patient";
        }

        var duration = action.DurationMinutes is > 0 and <= 180 ? action.DurationMinutes.Value : 30;
        var when = ParseWhen(action) ?? await _appointments.FindNextOpenSlotAsync(DateTime.Now, duration);
        var visitType = FirstNonEmpty(action.VisitType, action.Title, action.Category, "Follow-up")!;
        if (!MockAppointmentService.VisitTypes.Contains(visitType, StringComparer.OrdinalIgnoreCase))
        {
            visitType = "Follow-up";
        }

        var provider = FirstNonEmpty(action.Provider, action.AssignedTo, action.PrimaryProvider, patient.PrimaryProvider)
                       ?? MockAppointmentService.Providers[0];
        if (!MockAppointmentService.Providers.Contains(provider, StringComparer.OrdinalIgnoreCase))
        {
            provider = MockAppointmentService.Providers.FirstOrDefault(p =>
                           p.Contains(provider, StringComparison.OrdinalIgnoreCase))
                       ?? MockAppointmentService.Providers[0];
        }

        var location = FirstNonEmpty(action.Site, action.Organization, MockAppointmentService.Locations[0])!;
        if (!MockAppointmentService.Locations.Contains(location, StringComparer.OrdinalIgnoreCase))
        {
            location = MockAppointmentService.Locations[0];
        }

        var created = await _appointments.AddAppointmentAsync(new Appointment
        {
            PatientId = patient.Id,
            PatientName = patient.FullName,
            DateTime = when,
            DurationMinutes = duration,
            VisitType = visitType,
            Provider = provider,
            Location = location,
            Status = AppointmentStatus.Scheduled,
            Notes = action.Notes
        });

        return $"Scheduled {created.PatientName} {created.DateTime:ddd MMM d} at {created.DateTime:h:mm tt} ({created.VisitType}, {created.Provider})";
    }

    public async Task<string> CancelAsync(int patientId, ChartAskAction action)
    {
        var match = await FindAppointmentAsync(patientId, action);
        if (match == null)
        {
            return "skip: no matching appointment to cancel";
        }

        await _appointments.UpdateStatusAsync(match.Id, AppointmentStatus.Cancelled);
        return $"Cancelled {match.PatientName} on {match.DateTime:ddd MMM d} at {match.DateTime:h:mm tt}";
    }

    public async Task<string> RescheduleAsync(Patient patient, ChartAskAction action)
    {
        var match = await FindAppointmentAsync(patient.Id, action);
        if (match == null)
        {
            return "skip: no matching appointment to reschedule";
        }

        var when = ParseWhen(action) ?? await _appointments.FindNextOpenSlotAsync(DateTime.Now, match.DurationMinutes);
        match.DateTime = when;
        if (!string.IsNullOrWhiteSpace(action.VisitType)) match.VisitType = action.VisitType;
        if (!string.IsNullOrWhiteSpace(action.Provider)) match.Provider = action.Provider;
        if (!string.IsNullOrWhiteSpace(action.Site)) match.Location = action.Site;
        if (action.DurationMinutes is > 0) match.DurationMinutes = action.DurationMinutes.Value;
        await _appointments.UpdateAppointmentAsync(match);
        return $"Rescheduled {patient.FullName} to {when:ddd MMM d} at {when:h:mm tt}";
    }

    private async Task<Appointment?> FindAppointmentAsync(int patientId, ChartAskAction action)
    {
        var list = (await _appointments.GetAppointmentsByPatientAsync(patientId))
            .Where(a => a.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.Completed and not AppointmentStatus.NoShow)
            .OrderBy(a => a.DateTime)
            .ToList();
        if (list.Count == 0)
        {
            return null;
        }

        var when = ParseWhen(action);
        if (when != null)
        {
            return list.FirstOrDefault(a => a.DateTime == when)
                   ?? list.FirstOrDefault(a => a.DateTime.Date == when.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(action.VisitType) || !string.IsNullOrWhiteSpace(action.Name))
        {
            var label = action.VisitType ?? action.Name!;
            return list.FirstOrDefault(a => a.VisitType.Contains(label, StringComparison.OrdinalIgnoreCase));
        }

        return list.FirstOrDefault(a => a.DateTime >= DateTime.Now) ?? list.Last();
    }

    public static Patient? ResolveNamedPatient(string question, IReadOnlyList<Patient> roster)
    {
        var matches = roster
            .Where(p => PatientAskGuard.MentionsNamedPatient(question, [p]))
            .DistinctBy(p => p.Id)
            .ToList();
        return matches.Count == 1 ? matches[0] : null;
    }

    private static Patient? ResolvePatient(ChartAskAction action, IReadOnlyList<Patient> roster, int? defaultPatientId)
    {
        if (action.PatientId is int id && id > 0)
        {
            var byId = roster.FirstOrDefault(p => p.Id == id);
            if (byId != null)
            {
                return byId;
            }
        }

        foreach (var label in new[] { action.PatientName, action.Name })
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            var match = roster.FirstOrDefault(p => p.FullName.Equals(label.Trim(), StringComparison.OrdinalIgnoreCase))
                        ?? roster.FirstOrDefault(p =>
                            p.FirstName.Length >= 2
                            && p.LastName.Length >= 2
                            && label.Contains(p.FirstName, StringComparison.OrdinalIgnoreCase)
                            && label.Contains(p.LastName, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                return match;
            }
        }

        return defaultPatientId is int fallback
            ? roster.FirstOrDefault(p => p.Id == fallback)
            : null;
    }

    private static DateTime? ParseWhen(ChartAskAction action)
    {
        foreach (var raw in new[] { action.ScheduledAt, action.DateTime, action.DueDate, action.OnsetDate })
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var parsed))
            {
                if (parsed.TimeOfDay == TimeSpan.Zero && !string.IsNullOrWhiteSpace(action.Time)
                    && TimeSpan.TryParse(action.Time, CultureInfo.InvariantCulture, out var time))
                {
                    return parsed.Date.Add(time);
                }

                if (parsed.TimeOfDay == TimeSpan.Zero && raw.Trim().Length <= 10)
                {
                    continue;
                }

                return parsed;
            }
        }

        if (!string.IsNullOrWhiteSpace(action.Time)
            && TimeSpan.TryParse(action.Time, CultureInfo.InvariantCulture, out var todayTime))
        {
            return DateTime.Today.Add(todayTime);
        }

        return null;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
}

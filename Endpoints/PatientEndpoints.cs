using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Logging;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof_EMR.Endpoints;

public static class PatientEndpoints
{
    public static void MapPatientEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var patientGroup = endpoints.MapGroup("/api/patients")
            .RequireAuthorization()
            .WithTags("Patients");

        patientGroup.MapGet("/", GetPatients)
            .WithName("GetPatients")
            .WithSummary("Get all patients")
            .WithDescription("Retrieves a paginated list of patients");

        patientGroup.MapGet("/{id}", GetPatientById)
            .WithName("GetPatientById")
            .WithSummary("Get patient by ID")
            .WithDescription("Retrieves a specific patient by their ID");

        patientGroup.MapPost("/", CreatePatient)
            .RequireAuthorization("Physician", "Admin")
            .WithName("CreatePatient")
            .WithSummary("Create new patient")
            .WithDescription("Creates a new patient record");

        patientGroup.MapPut("/{id}", UpdatePatient)
            .RequireAuthorization("Physician", "Admin")
            .WithName("UpdatePatient")
            .WithSummary("Update patient")
            .WithDescription("Updates an existing patient record");

        patientGroup.MapDelete("/{id}", DeletePatient)
            .RequireAuthorization("Admin")
            .WithName("DeletePatient")
            .WithSummary("Delete patient")
            .WithDescription("Deletes a patient record");

        patientGroup.MapGet("/{id}/appointments", GetPatientAppointments)
            .WithName("GetPatientAppointments")
            .WithSummary("Get patient appointments")
            .WithDescription("Retrieves all appointments for a specific patient");

        patientGroup.MapGet("/search", SearchPatients)
            .WithName("SearchPatients")
            .WithSummary("Search patients")
            .WithDescription("Searches patients by name, MRN, or other criteria");
    }

    private static async Task<IResult> GetPatients(
        [FromServices] ApplicationDbContext dbContext,
        [FromServices] ILoggerFactory loggerFactory,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var log = loggerFactory.CreateLogger("Zebrahoof_EMR.Api.Patients");
        var query = dbContext.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.FirstName.Contains(search) ||
                p.LastName.Contains(search) ||
                p.MRN.Contains(search) ||
                (p.Email != null && p.Email.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var patients = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        log.LogInformation(
            "Listed patients page {Page} size {PageSize} total {TotalCount} hasSearchFilter {HasSearch}",
            page,
            pageSize,
            totalCount,
            !string.IsNullOrWhiteSpace(search));

        return Results.Ok(new PatientListResponse
        {
            Patients = patients,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    private static async Task<IResult> GetPatientById(
        HttpContext http,
        [FromServices] ApplicationDbContext dbContext,
        [FromServices] IAuditLogger audit,
        [FromServices] ILoggerFactory loggerFactory,
        int id)
    {
        var log = loggerFactory.CreateLogger("Zebrahoof_EMR.Api.Patients");
        var patient = await dbContext.Patients.FindAsync(id);

        if (patient == null)
        {
            log.LogWarning("GetPatientById: patient {PatientId} not found", id);
            return Results.NotFound(new { Message = $"Patient with ID {id} not found" });
        }

        await EndpointAuditHelper.AuditAsync(audit, http, "patient_view", $"patient:{id}", new { patientId = id });
        return Results.Ok(patient);
    }

    private static async Task<IResult> CreatePatient(
        HttpContext http,
        [FromServices] ApplicationDbContext dbContext,
        [FromServices] IAuditLogger audit,
        [FromServices] ILoggerFactory loggerFactory,
        [FromBody] CreatePatientRequest request)
    {
        var log = loggerFactory.CreateLogger("Zebrahoof_EMR.Api.Patients");
        if (!IsValidPatientRequest(request))
        {
            log.LogWarning("CreatePatient: validation failed");
            return Results.BadRequest(new { Message = "Invalid patient data", Errors = GetValidationErrors(request) });
        }

        var existingPatient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.MRN == request.MRN);

        if (existingPatient != null)
        {
            log.LogWarning("CreatePatient: MRN conflict");
            return Results.Conflict(new { Message = $"Patient with MRN {request.MRN} already exists" });
        }

        var patient = new Patient
        {
            MRN = request.MRN,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            Sex = request.Sex,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            PrimaryProvider = request.PrimaryProvider,
            InsuranceName = request.InsuranceName,
            InsuranceId = request.InsuranceId,
            Allergies = request.Allergies ?? new List<string>(),
            Alerts = request.Alerts ?? new List<string>()
        };

        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync();

        log.LogInformation("CreatePatient: created patient id {PatientId}", patient.Id);
        await EndpointAuditHelper.AuditAsync(audit, http, "patient_create", $"patient:{patient.Id}", new { patientId = patient.Id });
        return Results.Created($"/api/patients/{patient.Id}", patient);
    }

    private static async Task<IResult> UpdatePatient(
        HttpContext http,
        [FromServices] ApplicationDbContext dbContext,
        [FromServices] IAuditLogger audit,
        [FromServices] ILoggerFactory loggerFactory,
        int id,
        [FromBody] UpdatePatientRequest request)
    {
        var log = loggerFactory.CreateLogger("Zebrahoof_EMR.Api.Patients");
        if (!IsValidUpdateRequest(request))
        {
            log.LogWarning("UpdatePatient: validation failed for id {PatientId}", id);
            return Results.BadRequest(new { Message = "Invalid patient data", Errors = GetUpdateValidationErrors(request) });
        }

        var patient = await dbContext.Patients.FindAsync(id);

        if (patient == null)
        {
            log.LogWarning("UpdatePatient: patient {PatientId} not found", id);
            return Results.NotFound(new { Message = $"Patient with ID {id} not found" });
        }

        if (request.FirstName != null) patient.FirstName = request.FirstName;
        if (request.LastName != null) patient.LastName = request.LastName;
        if (request.Phone != null) patient.Phone = request.Phone;
        if (request.Email != null) patient.Email = request.Email;
        if (request.Address != null) patient.Address = request.Address;
        if (request.City != null) patient.City = request.City;
        if (request.State != null) patient.State = request.State;
        if (request.ZipCode != null) patient.ZipCode = request.ZipCode;
        if (request.PrimaryProvider != null) patient.PrimaryProvider = request.PrimaryProvider;
        if (request.InsuranceName != null) patient.InsuranceName = request.InsuranceName;
        if (request.InsuranceId != null) patient.InsuranceId = request.InsuranceId;
        if (request.Allergies != null) patient.Allergies = request.Allergies;
        if (request.Alerts != null) patient.Alerts = request.Alerts;

        await dbContext.SaveChangesAsync();

        log.LogInformation("UpdatePatient: updated patient id {PatientId}", id);
        await EndpointAuditHelper.AuditAsync(audit, http, "patient_update", $"patient:{id}", new { patientId = id });
        return Results.Ok(patient);
    }

    private static async Task<IResult> DeletePatient(
        HttpContext http,
        [FromServices] ApplicationDbContext dbContext,
        [FromServices] IAuditLogger audit,
        [FromServices] ILoggerFactory loggerFactory,
        int id)
    {
        var log = loggerFactory.CreateLogger("Zebrahoof_EMR.Api.Patients");
        var patient = await dbContext.Patients.FindAsync(id);

        if (patient == null)
        {
            log.LogWarning("DeletePatient: patient {PatientId} not found", id);
            return Results.NotFound(new { Message = $"Patient with ID {id} not found" });
        }

        dbContext.Patients.Remove(patient);
        await dbContext.SaveChangesAsync();

        log.LogWarning("DeletePatient: deleted patient id {PatientId}", id);
        await EndpointAuditHelper.AuditAsync(audit, http, "patient_delete", $"patient:{id}", new { patientId = id });
        return Results.NoContent();
    }

    private static async Task<IResult> GetPatientAppointments(
        HttpContext http,
        [FromServices] ApplicationDbContext dbContext,
        [FromServices] IAuditLogger audit,
        [FromServices] ILoggerFactory loggerFactory,
        int id,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var log = loggerFactory.CreateLogger("Zebrahoof_EMR.Api.Patients");
        var patientExists = await dbContext.Patients.AnyAsync(p => p.Id == id);
        if (!patientExists)
        {
            log.LogWarning("GetPatientAppointments: patient {PatientId} not found", id);
            return Results.NotFound(new { Message = $"Patient with ID {id} not found" });
        }

        var query = dbContext.Appointments.Where(a => a.PatientId == id);

        if (fromDate.HasValue)
        {
            query = query.Where(a => a.DateTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(a => a.DateTime <= toDate.Value);
        }

        var appointments = await query
            .OrderBy(a => a.DateTime)
            .ToListAsync();

        await EndpointAuditHelper.AuditAsync(
            audit,
            http,
            "patient_appointments_view",
            $"patient:{id}",
            new { patientId = id, count = appointments.Count });
        return Results.Ok(appointments);
    }

    private static async Task<IResult> SearchPatients(
        HttpContext http,
        [FromServices] ApplicationDbContext dbContext,
        [FromServices] IAuditLogger audit,
        [FromServices] ILoggerFactory loggerFactory,
        [FromQuery] string q,
        [FromQuery] int limit = 20)
    {
        var log = loggerFactory.CreateLogger("Zebrahoof_EMR.Api.Patients");
        if (string.IsNullOrWhiteSpace(q))
        {
            log.LogWarning("SearchPatients: missing query parameter");
            return Results.BadRequest(new { Message = "Search query 'q' is required" });
        }

        var patients = await dbContext.Patients
            .Where(p =>
                p.FirstName.Contains(q) ||
                p.LastName.Contains(q) ||
                p.MRN.Contains(q) ||
                (p.Email != null && p.Email.Contains(q)))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Take(limit)
            .ToListAsync();

        log.LogInformation("SearchPatients: term length {TermLength} limit {Limit} results {Count}", q.Length, limit, patients.Count);
        await EndpointAuditHelper.AuditAsync(
            audit,
            http,
            "patient_search",
            "patient",
            new { termLength = q.Length, limit, resultCount = patients.Count });
        return Results.Ok(patients);
    }

    private static bool IsValidPatientRequest(CreatePatientRequest request)
    {
        return !string.IsNullOrWhiteSpace(request.MRN) &&
               !string.IsNullOrWhiteSpace(request.FirstName) &&
               !string.IsNullOrWhiteSpace(request.LastName) &&
               request.DateOfBirth != default &&
               !string.IsNullOrWhiteSpace(request.Sex) &&
               new[] { "M", "F", "O" }.Contains(request.Sex.ToUpper());
    }

    private static bool IsValidUpdateRequest(UpdatePatientRequest request)
    {
        if (request.Sex != null && !new[] { "M", "F", "O" }.Contains(request.Sex.ToUpper()))
        {
            return false;
        }

        return true;
    }

    private static List<string> GetValidationErrors(CreatePatientRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.MRN))
            errors.Add("MRN is required");

        if (string.IsNullOrWhiteSpace(request.FirstName))
            errors.Add("First name is required");

        if (string.IsNullOrWhiteSpace(request.LastName))
            errors.Add("Last name is required");

        if (request.DateOfBirth == default)
            errors.Add("Date of birth is required");

        if (string.IsNullOrWhiteSpace(request.Sex))
            errors.Add("Sex is required");
        else if (!new[] { "M", "F", "O" }.Contains(request.Sex.ToUpper()))
            errors.Add("Sex must be M, F, or O");

        return errors;
    }

    private static List<string> GetUpdateValidationErrors(UpdatePatientRequest request)
    {
        var errors = new List<string>();

        if (request.Sex != null && !new[] { "M", "F", "O" }.Contains(request.Sex.ToUpper()))
            errors.Add("Sex must be M, F, or O");

        return errors;
    }
}

// Request/Response DTOs
public class PatientListResponse
{
    public List<Patient> Patients { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class CreatePatientRequest
{
    public string MRN { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Sex { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? PrimaryProvider { get; set; }
    public string? InsuranceName { get; set; }
    public string? InsuranceId { get; set; }
    public List<string>? Allergies { get; set; }
    public List<string>? Alerts { get; set; }
}

public class UpdatePatientRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? PrimaryProvider { get; set; }
    public string? InsuranceName { get; set; }
    public string? InsuranceId { get; set; }
    public string? Sex { get; set; }
    public List<string>? Allergies { get; set; }
    public List<string>? Alerts { get; set; }
}

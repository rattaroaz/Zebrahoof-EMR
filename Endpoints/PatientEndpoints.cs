using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;

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
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
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
        [FromServices] ApplicationDbContext dbContext,
        int id)
    {
        var patient = await dbContext.Patients.FindAsync(id);
        
        if (patient == null)
        {
            return Results.NotFound(new { Message = $"Patient with ID {id} not found" });
        }

        return Results.Ok(patient);
    }

    private static async Task<IResult> CreatePatient(
        [FromServices] ApplicationDbContext dbContext,
        [FromBody] CreatePatientRequest request)
    {
        if (!IsValidPatientRequest(request))
        {
            return Results.BadRequest(new { Message = "Invalid patient data", Errors = GetValidationErrors(request) });
        }

        // Check if MRN already exists
        var existingPatient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.MRN == request.MRN);
        
        if (existingPatient != null)
        {
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

        return Results.Created($"/api/patients/{patient.Id}", patient);
    }

    private static async Task<IResult> UpdatePatient(
        [FromServices] ApplicationDbContext dbContext,
        int id,
        [FromBody] UpdatePatientRequest request)
    {
        if (!IsValidUpdateRequest(request))
        {
            return Results.BadRequest(new { Message = "Invalid patient data", Errors = GetUpdateValidationErrors(request) });
        }

        var patient = await dbContext.Patients.FindAsync(id);
        
        if (patient == null)
        {
            return Results.NotFound(new { Message = $"Patient with ID {id} not found" });
        }

        // Update properties
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

        return Results.Ok(patient);
    }

    private static async Task<IResult> DeletePatient(
        [FromServices] ApplicationDbContext dbContext,
        int id)
    {
        var patient = await dbContext.Patients.FindAsync(id);
        
        if (patient == null)
        {
            return Results.NotFound(new { Message = $"Patient with ID {id} not found" });
        }

        dbContext.Patients.Remove(patient);
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> GetPatientAppointments(
        [FromServices] ApplicationDbContext dbContext,
        int id,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var patientExists = await dbContext.Patients.AnyAsync(p => p.Id == id);
        if (!patientExists)
        {
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

        return Results.Ok(appointments);
    }

    private static async Task<IResult> SearchPatients(
        [FromServices] ApplicationDbContext dbContext,
        [FromQuery] string q,
        [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
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
        // For updates, we only validate that if fields are provided, they're valid
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

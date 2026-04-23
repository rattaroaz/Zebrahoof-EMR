using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;

namespace Zebrahoof.EMR.UiSmokeTests;

public static class UITestDataSeeder
{
    public static async Task SeedAllTestDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Clear existing test data to ensure clean state
        await ClearTestDataAsync(dbContext, userManager);

        // Seed test users
        await SeedTestUsersAsync(userManager, dbContext);
        
        // Seed test patients
        await SeedTestPatientsAsync(dbContext);
        
        // Seed appointments
        await SeedTestAppointmentsAsync(dbContext);
        
        // Seed clinical tasks
        await SeedTestClinicalTasksAsync(dbContext);
        
        // Seed messages
        await SeedTestMessagesAsync(dbContext);
        
        await dbContext.SaveChangesAsync();
    }

    private static async Task ClearTestDataAsync(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        // Clear in correct order to avoid foreign key constraints
        dbContext.ClinicalTasks.RemoveRange(dbContext.ClinicalTasks.Where(t => t.Title.Contains("Test") || t.Description.Contains("Test")));
        dbContext.Messages.RemoveRange(dbContext.Messages.Where(m => m.Subject.Contains("Test") || m.Body.Contains("Test")));
        dbContext.Appointments.RemoveRange(dbContext.Appointments.Where(a => a.Notes != null && a.Notes.Contains("Test")));
        dbContext.Patients.RemoveRange(dbContext.Patients.Where(p => p.Email.Contains("@testemail.com")));
        
        // Clear test users (keep non-test users)
        var testUsers = await dbContext.Users.Where(u => 
            u.UserName.StartsWith("test_") || 
            u.UserName == "playwright" ||
            u.Email.Contains("@testemail.com")).ToListAsync();
        
        foreach (var user in testUsers)
        {
            await userManager.DeleteAsync(user);
        }
        
        // Clear test audit logs
        dbContext.AuditLogs.RemoveRange(dbContext.AuditLogs.Where(al => al.Scope.Contains("ui_test")));
        
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedTestUsersAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext)
    {
        var testUsers = TestDataFactory.Users.CreateTestUsers();
        
        foreach (var user in testUsers)
        {
            var existingUser = await userManager.FindByNameAsync(user.UserName!);
            if (existingUser == null)
            {
                var createResult = await userManager.CreateAsync(user, "TestPassword123!");
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create test user {user.UserName}: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
                
                // Add roles based on username
                var role = user.UserName switch
                {
                    "playwright" or "test_physician" => "Physician",
                    "test_nurse" => "Nurse",
                    "test_admin" => "Admin",
                    "test_receptionist" => "Scheduler",
                    _ => "Patient"
                };
                
                await userManager.AddToRoleAsync(user, role);
                
                // Create user profile
                dbContext.UserProfiles.Add(new UserProfile
                {
                    UserId = user.Id,
                    DisplayName = user.DisplayName,
                    Department = role switch
                    {
                        "Physician" => "Clinical",
                        "Nurse" => "Clinical",
                        "Admin" => "Administration",
                        "Scheduler" => "Operations",
                        _ => "General"
                    }
                });
                
                // Create audit log entry
                dbContext.AuditLogs.Add(new AuditLog
                {
                    Action = "user_created",
                    Scope = "ui_test_setup",
                    UserId = user.Id,
                    Timestamp = DateTimeOffset.UtcNow,
                    Metadata = $"{{\"purpose\":\"ui_test\",\"username\":\"{user.UserName}\",\"role\":\"{role}\"}}"
                });
            }
        }
    }

    private static async Task SeedTestPatientsAsync(ApplicationDbContext dbContext)
    {
        var patients = TestDataFactory.Patients.CreateTestPatients(15); // Create 15 test patients
        
        foreach (var patient in patients)
        {
            dbContext.Patients.Add(patient);
        }
        
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedTestAppointmentsAsync(ApplicationDbContext dbContext)
    {
        var patients = await dbContext.Patients.ToListAsync();
        
        foreach (var patient in patients)
        {
            var appointments = TestDataFactory.Appointments.CreateTestAppointments(patient.Id, 3);
            foreach (var appointment in appointments)
            {
                dbContext.Appointments.Add(appointment);
            }
        }
        
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedTestClinicalTasksAsync(ApplicationDbContext dbContext)
    {
        var tasks = new List<ClinicalTask>();
        var assignedToOptions = new[] { "playwright", "test_physician", "test_nurse" };
        var createdByOptions = new[] { "test_admin", "test_receptionist" };
        var taskTypes = new[] { ClinicalTaskType.ReviewResults, ClinicalTaskType.PhoneCall, ClinicalTaskType.Other, ClinicalTaskType.ReviewResults, ClinicalTaskType.PhoneCall };
        var statuses = new[] { ClinicalTaskStatus.Pending, ClinicalTaskStatus.InProgress, ClinicalTaskStatus.Completed, ClinicalTaskStatus.Cancelled };
        var priorities = new[] { ClinicalTaskPriority.High, ClinicalTaskPriority.Normal, ClinicalTaskPriority.Low, ClinicalTaskPriority.Urgent };
        
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(new ClinicalTask
            {
                Title = $"Test Task: {taskTypes[i % taskTypes.Length]} {i + 1}",
                Description = $"This is a test task for {taskTypes[i % taskTypes.Length]} requiring attention.",
                Type = taskTypes[i % taskTypes.Length],
                AssignedTo = assignedToOptions[i % assignedToOptions.Length],
                CreatedBy = createdByOptions[i % createdByOptions.Length],
                Status = statuses[i % statuses.Length],
                Priority = priorities[i % priorities.Length],
                DueDate = DateTime.UtcNow.AddDays(i % 7 == 0 ? -1 : i + 1), // Some overdue, some future
                CreatedAt = DateTime.UtcNow.AddHours(-i * 2)
            });
        }
        
        foreach (var task in tasks)
        {
            dbContext.ClinicalTasks.Add(task);
        }
        
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedTestMessagesAsync(ApplicationDbContext dbContext)
    {
        var messages = new List<Message>();
        var fromOptions = new[] { "test_admin", "test_receptionist", "test_physician" };
        var toOptions = new[] { "playwright", "test_physician", "test_nurse" };
        var subjects = new[] { "System Update", "Patient Inquiry", "Lab Result", "Schedule Change", "Administrative Notice" };
        var messageTypes = new[] { MessageType.Administrative, MessageType.PatientPortal, MessageType.Results, MessageType.Administrative, MessageType.General };
        
        for (int i = 0; i < 8; i++)
        {
            messages.Add(new Message
            {
                Subject = $"Test: {subjects[i % subjects.Length]}",
                Body = $"This is a test message regarding {subjects[i % subjects.Length].ToLower()}. Please review and take appropriate action.",
                From = fromOptions[i % fromOptions.Length],
                To = toOptions[i % toOptions.Length],
                IsRead = i % 2 == 0,
                Type = messageTypes[i % messageTypes.Length],
                SentAt = DateTime.UtcNow.AddHours(-i * 3)
            });
        }
        
        foreach (var message in messages)
        {
            dbContext.Messages.Add(message);
        }
        
        await dbContext.SaveChangesAsync();
    }
}

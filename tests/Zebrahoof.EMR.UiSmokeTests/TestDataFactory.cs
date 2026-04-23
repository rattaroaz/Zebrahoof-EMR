using System;
using System.Collections.Generic;
using Zebrahoof_EMR.Models;

namespace Zebrahoof.EMR.UiSmokeTests;

public static class TestDataFactory
{
    private static readonly Random _random = new(42); // Fixed seed for reproducible tests

    public static class Patients
    {
        public static Patient CreateTestPatient(int index)
        {
            var firstNames = new[] { "John", "Jane", "Robert", "Mary", "Michael", "Sarah", "David", "Emily", "James", "Lisa" };
            var lastNames = new[] { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
            var sexes = new[] { "M", "F" };
            
            return new Patient
            {
                Id = index,
                MRN = $"MRN{index:D6}",
                FirstName = firstNames[index % firstNames.Length],
                LastName = lastNames[index % lastNames.Length],
                DateOfBirth = DateTime.Today.AddDays(-_random.Next(365 * 18, 365 * 80)), // 18-80 years old
                Sex = sexes[index % sexes.Length],
                Phone = $"555-0{index:D2}-555-{_random.Next(1000, 9999)}",
                Email = $"patient{index}@testemail.com",
                Address = $"{_random.Next(100, 999)} Main St",
                City = "Test City",
                State = "TC",
                ZipCode = $"{_random.Next(10000, 99999)}",
                PrimaryProvider = $"Dr. {firstNames[(index + 1) % firstNames.Length]} {lastNames[(index + 1) % lastNames.Length]}",
                InsuranceName = "Test Insurance Co",
                InsuranceId = $"INS{index:D8}",
                Allergies = new List<string> { "Penicillin", "Pollen", "Dust" }.Take(index % 3 + 1).ToList(),
                Alerts = new List<string> { "Diabetes", "Hypertension" }.Take(index % 2).ToList(),
                LastVisit = DateTime.Today.AddDays(-_random.Next(1, 90))
            };
        }

        public static List<Patient> CreateTestPatients(int count = 10)
        {
            var patients = new List<Patient>();
            for (int i = 1; i <= count; i++)
            {
                patients.Add(CreateTestPatient(i));
            }
            return patients;
        }
    }

    public static class Appointments
    {
        public static Appointment CreateTestAppointment(int patientId, int index)
        {
            var visitTypes = new[] { "Checkup", "Follow-up", "Consultation", "Urgent Care", "Specialist Visit" };
            var statuses = new[] { AppointmentStatus.Scheduled, AppointmentStatus.Completed, AppointmentStatus.Cancelled, AppointmentStatus.NoShow };
            var providers = new[] { "Dr. Smith", "Dr. Johnson", "Dr. Williams", "Dr. Brown" };
            var locations = new[] { "Main Clinic", "Urgent Care", "Specialty Center" };
            
            return new Appointment
            {
                Id = index,
                PatientId = patientId,
                PatientName = $"Test Patient {patientId}",
                DateTime = DateTime.UtcNow.AddDays(_random.Next(-30, 30)),
                DurationMinutes = 30 + (_random.Next(0, 4) * 15), // 30-90 minutes
                VisitType = visitTypes[index % visitTypes.Length],
                Provider = providers[index % providers.Length],
                Location = locations[index % locations.Length],
                Status = statuses[index % statuses.Length],
                Notes = $"Test appointment notes for patient {patientId}"
            };
        }

        public static List<Appointment> CreateTestAppointments(int patientId, int count = 3)
        {
            var appointments = new List<Appointment>();
            for (int i = 0; i < count; i++)
            {
                appointments.Add(CreateTestAppointment(patientId, i));
            }
            return appointments;
        }
    }

    public static class ClinicalData
    {
        public static Encounter CreateTestEncounter(int patientId, int index)
        {
            var visitTypes = new[] { "Office Visit", "Telehealth", "Emergency", "Consultation" };
            var providers = new[] { "Dr. Smith", "Dr. Johnson", "Dr. Williams" };
            var locations = new[] { "Main Clinic", "Urgent Care", "Emergency Department" };
            var statuses = new[] { EncounterStatus.InProgress, EncounterStatus.Signed, EncounterStatus.Addended };
            
            return new Encounter
            {
                Id = index,
                PatientId = patientId,
                PatientName = $"Test Patient {patientId}",
                DateTime = DateTime.UtcNow.AddDays(-_random.Next(0, 365)),
                VisitType = visitTypes[index % visitTypes.Length],
                Provider = providers[index % providers.Length],
                Location = locations[index % locations.Length],
                Status = statuses[index % statuses.Length],
                ChiefComplaint = $"Test chief complaint for encounter {index}",
                Assessment = $"Test assessment for encounter {index}",
                Plan = $"Test plan for encounter {index}"
            };
        }

        public static List<Encounter> CreateTestEncounters(int patientId, int count = 5)
        {
            var encounters = new List<Encounter>();
            for (int i = 0; i < count; i++)
            {
                encounters.Add(CreateTestEncounter(patientId, i));
            }
            return encounters;
        }

        public static Problem CreateTestProblem(int patientId, int index)
        {
            var problems = new[] { "Hypertension", "Diabetes Type 2", "Asthma", "Arthritis", "Hyperlipidemia" };
            var icdCodes = new[] { "I10", "E11.9", "J45.909", "M15.9", "E78.5" };
            var severities = new[] { "Mild", "Moderate", "Severe" };
            var statuses = new[] { ProblemStatus.Active, ProblemStatus.Resolved, ProblemStatus.Inactive };
            
            return new Problem
            {
                Id = index,
                PatientId = patientId,
                Name = problems[index % problems.Length],
                IcdCode = icdCodes[index % icdCodes.Length],
                OnsetDate = DateTime.UtcNow.AddDays(-_random.Next(30, 3650)),
                ResolvedDate = statuses[index % statuses.Length] == ProblemStatus.Resolved ? DateTime.UtcNow.AddDays(-_random.Next(1, 30)) : null,
                Status = statuses[index % statuses.Length],
                Severity = severities[index % severities.Length],
                Notes = $"Test problem notes for {problems[index % problems.Length]}"
            };
        }

        public static List<Problem> CreateTestProblems(int patientId, int count = 3)
        {
            var problems = new List<Problem>();
            for (int i = 0; i < count; i++)
            {
                problems.Add(CreateTestProblem(patientId, i));
            }
            return problems;
        }

        public static Medication CreateTestMedication(int patientId, int index)
        {
            var medications = new[] { "Lisinopril", "Metformin", "Albuterol", "Ibuprofen", "Atorvastatin" };
            var doses = new[] { "10mg", "500mg", "90mcg", "200mg", "20mg" };
            var routes = new[] { "Oral", "Inhaled", "Topical", "IV" };
            var frequencies = new[] { "Once daily", "Twice daily", "As needed", "Three times daily" };
            var statuses = new[] { MedicationStatus.Active, MedicationStatus.Discontinued, MedicationStatus.OnHold };
            
            return new Medication
            {
                Id = index,
                PatientId = patientId,
                Name = medications[index % medications.Length],
                Dose = doses[index % doses.Length],
                Route = routes[index % routes.Length],
                Frequency = frequencies[index % frequencies.Length],
                StartDate = DateTime.UtcNow.AddDays(-_random.Next(1, 365)),
                EndDate = statuses[index % statuses.Length] == MedicationStatus.Discontinued ? DateTime.UtcNow.AddDays(-_random.Next(1, 30)) : null,
                Prescriber = $"Dr. Smith",
                Status = statuses[index % statuses.Length],
                IsHighRisk = index % 5 == 0, // 1 in 5 are high risk
                IsLongTerm = index % 3 != 0, // 2 in 3 are long-term
                Instructions = $"Take with food" + (index % 2 == 0 ? " as needed" : ""),
                RefillsRemaining = index % 2 == 0 ? 3 : null,
                DaysSupply = index % 2 == 0 ? 30 : null,
                Pharmacy = "Test Pharmacy"
            };
        }

        public static List<Medication> CreateTestMedications(int patientId, int count = 5)
        {
            var medications = new List<Medication>();
            for (int i = 0; i < count; i++)
            {
                medications.Add(CreateTestMedication(patientId, i));
            }
            return medications;
        }
    }

    public static class Users
    {
        public static ApplicationUser CreateTestUser(string username, string role = "Physician")
        {
            var nameParts = username.Split('_');
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : "User";
            
            return new ApplicationUser
            {
                UserName = username,
                Email = $"{username}@testemail.com",
                FirstName = firstName,
                LastName = lastName,
                DisplayName = $"{firstName} {lastName}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(1, 365)),
                EmailConfirmed = true
            };
        }

        public static List<ApplicationUser> CreateTestUsers()
        {
            return new List<ApplicationUser>
            {
                CreateTestUser("playwright", "Physician"),
                CreateTestUser("test_physician", "Physician"),
                CreateTestUser("test_nurse", "Nurse"),
                CreateTestUser("test_admin", "Admin"),
                CreateTestUser("test_receptionist", "Receptionist")
            };
        }
    }
}

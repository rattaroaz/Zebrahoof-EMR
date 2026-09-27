using FluentAssertions;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Services;

namespace Zebrahoof.EMR.MutationTests;

/// <summary>
/// Tight checks on logic that must not silently invert.
/// </summary>
public class CriticalBusinessLogicMutationTests
{
    [Fact]
    public void PatientAge_UsesCompletedBirthdaysOnly()
    {
        var today = DateTime.Today;
        new Patient { DateOfBirth = today }.Age.Should().Be(0);
        new Patient { DateOfBirth = today.AddYears(-34) }.Age.Should().Be(34);

        var tomorrow = today.AddDays(1);
        if (tomorrow.Year == today.Year)
        {
            var notYet = new DateTime(today.Year - 34, tomorrow.Month, tomorrow.Day);
            new Patient { DateOfBirth = notYet }.Age.Should().Be(33);
        }
    }

    [Fact]
    public void ChartPrivilege_DoesNotAllowFrontDeskToPrescribe()
    {
        var privileges = new ChartPrivilegeService();
        privileges.Evaluate(UserRole.FrontDesk, "prescribe").Allowed.Should().BeFalse();
        privileges.Evaluate(UserRole.Physician, "prescribe").Allowed.Should().BeTrue();
    }
}

using Zebrahoof_EMR.Models;

namespace Zebrahoof.EMR.UnitTests;

public class PatientAgeTests
{
    [Fact]
    public void Age_IsZeroOnBirthDate()
    {
        Assert.Equal(0, new Patient { DateOfBirth = DateTime.Today }.Age);
    }

    [Fact]
    public void Age_WaitsUntilBirthdayHasOccurred()
    {
        var today = DateTime.Today;
        Assert.Equal(20, new Patient { DateOfBirth = today.AddYears(-20) }.Age);

        var tomorrow = today.AddDays(1);
        if (tomorrow.Year != today.Year)
        {
            return;
        }

        var notYet = new DateTime(today.Year - 20, tomorrow.Month, tomorrow.Day);
        Assert.Equal(19, new Patient { DateOfBirth = notYet }.Age);
    }
}

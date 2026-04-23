namespace Zebrahoof_EMR.Configuration;

public sealed class PlaywrightTestUserOptions
{
    public bool Enabled { get; set; }
    public string UserName { get; set; } = "playwright";
    public string Email { get; set; } = "playwright@example.com";
    public string DisplayName { get; set; } = "Playwright Test User";
    public string Password { get; set; } = "P@ssw0rd!";
    public string[] Roles { get; set; } = ["Physician"];
    public bool ResetPasswordOnStartup { get; set; } = true;
}

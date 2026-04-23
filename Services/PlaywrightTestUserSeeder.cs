using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Zebrahoof_EMR.Configuration;
using Zebrahoof_EMR.Models;

namespace Zebrahoof_EMR.Services;

public class PlaywrightTestUserSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PlaywrightTestUserOptions _options;
    private readonly ILogger<PlaywrightTestUserSeeder> _logger;

    public PlaywrightTestUserSeeder(
        IServiceProvider serviceProvider,
        IOptions<PlaywrightTestUserOptions> options,
        ILogger<PlaywrightTestUserSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByNameAsync(_options.UserName);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = _options.UserName,
                Email = _options.Email,
                DisplayName = _options.DisplayName,
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(user, _options.Password);
            if (!createResult.Succeeded)
            {
                _logger.LogWarning("Failed to create Playwright test user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }
        }
        else if (_options.ResetPasswordOnStartup)
        {
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, resetToken, _options.Password);
            if (!resetResult.Succeeded)
            {
                _logger.LogWarning("Failed to reset password for Playwright test user: {Errors}", string.Join(", ", resetResult.Errors.Select(e => e.Description)));
            }
        }

        foreach (var role in _options.Roles ?? Array.Empty<string>())
        {
            if (!await userManager.IsInRoleAsync(user, role))
            {
                var result = await userManager.AddToRoleAsync(user, role);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed to add role {Role} to Playwright user: {Errors}", role, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

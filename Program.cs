using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using MudBlazor.Services;
using Zebrahoof_EMR.Components;
using Zebrahoof_EMR.Configuration;
using Zebrahoof_EMR.Services;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Models;
using Zebrahoof_EMR.Endpoints;
using Zebrahoof_EMR.Hubs;
using IOPath = System.IO.Path;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();

var sqliteConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
var postgresConnection = builder.Configuration.GetConnectionString("PostgresConnection");
var usePostgres = builder.Environment.IsProduction() && !string.IsNullOrWhiteSpace(postgresConnection);

Directory.CreateDirectory(IOPath.Combine(builder.Environment.ContentRootPath, "App_Data"));

builder.Services.AddDbContext<ApplicationDbContext>(ConfigureDatabase);
builder.Services.AddDistributedMemoryCache();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 1;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

const string LastActivityKey = ".zebrahoof_last_activity";
const string AbsoluteIssuedKey = ".zebrahoof_absolute_issue";

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "zebrahoof.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.SlidingExpiration = false;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.Events = new CookieAuthenticationEvents
    {
        OnSigningIn = context =>
        {
            var nowTicks = DateTimeOffset.UtcNow.Ticks.ToString();
            context.Properties.Items[LastActivityKey] = nowTicks;
            context.Properties.Items[AbsoluteIssuedKey] = nowTicks;
            return Task.CompletedTask;
        },
        OnValidatePrincipal = async context =>
        {
            var now = DateTimeOffset.UtcNow;
            var items = context.Properties.Items;

            if (items.TryGetValue(AbsoluteIssuedKey, out var absoluteTicks) &&
                long.TryParse(absoluteTicks, out var absTicks))
            {
                var absoluteIssued = new DateTimeOffset(absTicks, TimeSpan.Zero);
                if (now - absoluteIssued > TimeSpan.FromHours(12))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                    return;
                }
            }

            var idleWindow = ResolveIdleWindow(context.Principal);
            if (items.TryGetValue(LastActivityKey, out var lastActivityTicksString) &&
                long.TryParse(lastActivityTicksString, out var lastActivityTicks))
            {
                var lastActivity = new DateTimeOffset(lastActivityTicks, TimeSpan.Zero);
                if (now - lastActivity > idleWindow)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                    return;
                }
            }

            items[LastActivityKey] = now.Ticks.ToString();
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationConstants.ChartEditorPolicy,
        policy => policy.RequireRole("Physician", "Nurse"));
    options.AddPolicy(AuthorizationConstants.PrescriberPolicy,
        policy => policy.RequireRole("Physician"));
    options.AddPolicy(AuthorizationConstants.BillingPolicy,
        policy => policy.RequireClaim(AuthorizationConstants.BillingClaim, "true"));
});

builder.Services.AddSession(options =>
{
    options.Cookie.Name = "zebrahoof.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddSingleton<TimeProvider>(_ => TimeProvider.System);
builder.Services.AddSingleton<IClaimsTransformation, RoleClaimTransformation>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<PlaywrightTestUserOptions>(builder.Configuration.GetSection("PlaywrightTestUser"));
builder.Services.AddHostedService<PlaywrightTestUserSeeder>();

// Add MudBlazor services
builder.Services.AddMudServices();
builder.Services.AddSignalR();

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add mock data services
builder.Services.AddSingleton<AuthStateService>();
builder.Services.AddSingleton<AppStateService>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddScoped<StickyNoteService>();
builder.Services.AddScoped<PatientStickyNoteService>();
builder.Services.AddSingleton<MockPatientService>();
builder.Services.AddSingleton<MockAppointmentService>();
builder.Services.AddSingleton<MockTaskService>();
builder.Services.AddSingleton<MockMessageService>();
builder.Services.AddSingleton<MockClinicalDataService>();
builder.Services.AddHostedService<ClinicalDataPersistenceInitializer>();

// Add user mapping and auth sync services
builder.Services.AddScoped<UserMappingService>();
builder.Services.AddScoped<AuthSyncService>();
builder.Services.AddHttpClient<GrokApiService>(client =>
{
    // Multi-agent / reasoning runs can take many minutes; xAI docs suggest up to 3600s.
    client.Timeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddScoped<PatientRecordUpdateService>();
builder.Services.AddScoped<EncounterMessageService>();
builder.Services.AddScoped<GrokSessionStateService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<SessionHub>("/hubs/session");
app.MapAccountEndpoints();
app.MapPatientEndpoints();
app.MapSessionEndpoints();
app.MapDocumentEndpoints();

if (!usePostgres && app.Environment.IsProduction())
{
    app.Logger.LogWarning("Postgres connection string missing in production; the app is running against SQLite. Verify configuration.");
}

app.Run();

void ConfigureDatabase(DbContextOptionsBuilder options)
{
    if (!usePostgres)
    {
        options.UseSqlite(sqliteConnection);
        return;
    }

    options.UseNpgsql(postgresConnection!);
}

static TimeSpan ResolveIdleWindow(ClaimsPrincipal? principal)
{
    if (principal == null)
    {
        return TimeSpan.FromMinutes(15);
    }

    var adminRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "System Administrator"
    };

    return principal.Claims
        .Where(c => c.Type == ClaimTypes.Role)
        .Any(c => adminRoles.Contains(c.Value))
        ? TimeSpan.FromMinutes(30)
        : TimeSpan.FromMinutes(15);
}

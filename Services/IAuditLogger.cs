using System.Threading;
using System.Threading.Tasks;

namespace Zebrahoof_EMR.Services;

public interface IAuditLogger
{
    Task LogAsync(string action, string scope, string? metadata = null, string? userId = null, CancellationToken cancellationToken = default);
}

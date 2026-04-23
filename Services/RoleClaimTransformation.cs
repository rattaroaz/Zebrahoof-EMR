using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace Zebrahoof_EMR.Services;

public class RoleClaimTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity)
        {
            return Task.FromResult(principal);
        }

        var hasBillingRole = principal.Claims
            .Any(c => c.Type == ClaimTypes.Role &&
                      (c.Value.Equals("Billing", StringComparison.OrdinalIgnoreCase) ||
                       c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase)));

        if (hasBillingRole &&
            !identity.HasClaim(claim => claim.Type == AuthorizationConstants.BillingClaim))
        {
            identity.AddClaim(new Claim(AuthorizationConstants.BillingClaim, "true"));
        }

        return Task.FromResult(principal);
    }
}

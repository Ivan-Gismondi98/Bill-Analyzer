using System.Security.Claims;
using BollettaAnalyzer.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BollettaAnalyzer.Infrastructure.Auth;

/// <summary>Legge l'utente autenticato dal contesto HTTP corrente.</summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    public Guid? UtenteId
    {
        get
        {
            var value = _accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => _accessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}

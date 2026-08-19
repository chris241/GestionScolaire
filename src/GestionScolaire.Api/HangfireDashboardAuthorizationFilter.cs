using System.Text;
using Hangfire.Dashboard;

namespace GestionScolaire.Api;

/// Protège /hangfire par Basic Auth. Le dashboard est ouvert dans un onglet de navigateur (pas d'appel AJAX),
/// donc l'en-tête JWT "Authorization: Bearer" utilisé par le reste de l'API ne peut pas s'y appliquer :
/// Basic Auth est le mécanisme standard recommandé par Hangfire pour ce cas d'usage.
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string _username;
    private readonly string _password;

    public HangfireDashboardAuthorizationFilter(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var header = httpContext.Request.Headers.Authorization.ToString();

        if (header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..]));
                var separatorIndex = decoded.IndexOf(':');
                if (separatorIndex > 0)
                {
                    var username = decoded[..separatorIndex];
                    var password = decoded[(separatorIndex + 1)..];

                    if (username == _username && password == _password)
                        return true;
                }
            }
            catch (FormatException)
            {
                // En-tête Basic malformé : traité comme non authentifié ci-dessous.
            }
        }

        httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"GestionScolaire Hangfire\"";
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return false;
    }
}

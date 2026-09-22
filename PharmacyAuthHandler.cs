using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;


public class PharmacyAuthHandler :
AuthenticationHandler<AuthenticationSchemeOptions>
{
    public PharmacyAuthHandler (
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        :base(options,logger,encoder)
    {
        
    }
    protected override Task<AuthenticateResult> 
    HandleAuthenticateAsync ()
    {
        if(!Request.Headers.ContainsKey("x-Pharmacy-User"))
        {
            return Task.FromResult(
                AuthenticateResult.Fail("Missing Pharmacy user header.")
            );
        }
        var claims = new[]
        {
            new Claim(ClaimTypes.Name,Request.Headers["X-Pharmacy-User"]!)

        };
        var identity = new ClaimsIdentity(claims,Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal,Scheme.Name);
        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }

    
}
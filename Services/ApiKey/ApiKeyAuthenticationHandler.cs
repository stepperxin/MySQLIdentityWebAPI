﻿using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MySQLIdentityWebAPI.Data;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace MySQLIdentityWebAPI.Services.ApiKey
{
    class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        // https://gowthamcbe.com/2022/02/21/swagger-ui-for-api-key-authentication-flow-with-net-core-6/
        private const string API_KEY_HEADER = "ApiKey";

        private readonly ApplicationDBContext _context;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            ApplicationDBContext context
        ) : base(options, logger, encoder, clock)
        {
            _context = context;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(API_KEY_HEADER))
            {
                return AuthenticateResult.Fail("Header Not Found.");
            }

            string apiKeyToValidate = Request.Headers[API_KEY_HEADER];

            var apiKey = await _context.UserApiKeys
                .Include(uak => uak.User)
                .SingleOrDefaultAsync(uak => uak.Value == apiKeyToValidate);

            if (apiKey == null)
            {
                return AuthenticateResult.Fail("Invalid key.");
            }

            return AuthenticateResult.Success(CreateTicket(apiKey.User));
        }

        private AuthenticationTicket CreateTicket(IdentityUser user)
        {
            var claims = new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return ticket;
        }
    }
}

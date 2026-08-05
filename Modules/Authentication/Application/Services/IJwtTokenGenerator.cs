using System;
using System.Security.Claims;
using Modules.UserManagement.Domain.Entities;

namespace Modules.Authentication.Application.Services
{
    public interface IJwtTokenGenerator
    {
        string GenerateAccessToken(User user);
        string GenerateMfaChallengeToken(User user);
        ClaimsPrincipal? ValidateToken(string token);
    }
}

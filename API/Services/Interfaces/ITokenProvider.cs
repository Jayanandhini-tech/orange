using CMS.Dto;
using System.Security.Claims;

namespace CMS.API.Services.Interfaces;

public interface ITokenProvider
{
    Token GenerateJWTTokens(List<Claim> claims, int ExpiredMinutes = 30);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}

using CMS.API.Domains;

namespace CMS.API.Repository.IRepository;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{

    void RemoveAllTokens(string username);
    Task RemoveToken(string username, string token);
    void Update(RefreshToken obj);
}
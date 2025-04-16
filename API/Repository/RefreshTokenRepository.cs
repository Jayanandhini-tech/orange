using CMS.API.Domains;
using CMS.API.Repository.IRepository;
using CMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMS.API.Repository;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    private readonly AppDBContext db;

    public RefreshTokenRepository(AppDBContext db, ITenant tenant) : base(db, tenant)
    {
        this.db = db;
    }


    public void Update(RefreshToken obj)
    {
        db.RefreshTokens.Update(obj);
    }


    public async Task RemoveToken(string username, string token)
    {
        RefreshToken? tokenfromDb = await db.RefreshTokens.FirstOrDefaultAsync(x => x.UserName == username && x.Token == new Guid(token));
        if (tokenfromDb != null)
            db.RefreshTokens.Remove(tokenfromDb);
    }

    public void RemoveAllTokens(string username)
    {
        var tokens = db.RefreshTokens.Where(x => x.UserName == username);
        db.RefreshTokens.RemoveRange(tokens);
    }
}

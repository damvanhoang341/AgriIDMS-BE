// AgriIDMS.Infrastructure/Repositories/RefreshTokenRepository.cs
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using AgriIDMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgriIDMS.Infrastructure.Repositories;

public class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public Task AddAsync(RefreshToken token) => db.RefreshTokens.AddAsync(token).AsTask();
    public Task<RefreshToken?> GetByTokenAsync(string token) =>
        db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token);
    public void Update(RefreshToken token) => db.RefreshTokens.Update(token);
}

using Microsoft.EntityFrameworkCore;
using OnlyOfficeServer.Data;
using OnlyOfficeServer.Models;

namespace OnlyOfficeServer.Repositories;

public class InstallationRepository : IInstallationRepository
{
    private readonly AppDbContext _context;
    private bool _disposed = false;

    public InstallationRepository()
    {
        // Manual DbContext creation following existing OnlyOfficeRepository pattern
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=onlyoffice.db");
        _context = new AppDbContext(optionsBuilder.Options);
    }

    public async Task<Installation?> GetByApplicationIdAsync(int applicationId)
    {
        return await _context.Installations
            .FirstOrDefaultAsync(i => i.ApplicationId == applicationId);
    }

    public async Task<IEnumerable<Installation>> GetAllAsync()
    {
        return await _context.Installations.ToListAsync();
    }

    public async Task<Installation> AddAsync(Installation installation)
    {
        _context.Installations.Add(installation);
        await _context.SaveChangesAsync();
        return installation;
    }

    public async Task<Installation> UpdateAsync(Installation installation)
    {
        installation.UpdatedAt = DateTime.UtcNow;
        _context.Installations.Update(installation);
        await _context.SaveChangesAsync();
        return installation;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var installation = await _context.Installations.FindAsync(id);
        if (installation == null)
            return false;

        _context.Installations.Remove(installation);
        await _context.SaveChangesAsync();
        return true;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _context?.Dispose();
            _disposed = true;
        }
    }
}
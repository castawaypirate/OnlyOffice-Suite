using Microsoft.EntityFrameworkCore;
using OnlyOfficeServer.Data;
using OnlyOfficeServer.Models;

namespace OnlyOfficeServer.Repositories;

public class OnlyOfficeRepository : IOnlyOfficeRepository
{
    private readonly AppDbContext _context;
    private bool _disposed = false;

    public OnlyOfficeRepository()
    {
        // In .NET Framework 4.5.6, this would typically be:
        // _context = new ApplicationDbContext();
        // But for this POC we'll use the existing pattern
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite("Data Source=onlyoffice.db");
        _context = new AppDbContext(optionsBuilder.Options);
    }

    public async Task<FileEntity?> GetFileByIdAsync(Guid id)
    {
        return await _context.Files
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
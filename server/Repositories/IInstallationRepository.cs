using OnlyOfficeServer.Models;

namespace OnlyOfficeServer.Repositories;

public interface IInstallationRepository : IDisposable
{
    Task<Installation?> GetByApplicationIdAsync(int applicationId);
    Task<IEnumerable<Installation>> GetAllAsync();
    Task<Installation> AddAsync(Installation installation);
    Task<Installation> UpdateAsync(Installation installation);
    Task<bool> DeleteAsync(int id);
}
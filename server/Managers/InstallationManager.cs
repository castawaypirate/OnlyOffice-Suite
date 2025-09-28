using OnlyOfficeServer.Models;
using OnlyOfficeServer.Repositories;

namespace OnlyOfficeServer.Managers;

public class InstallationManager
{
    private readonly IInstallationRepository _repository;

    public InstallationManager(IInstallationRepository repository)
    {
        _repository = repository;
    }

    public async Task<Installation?> GetInstallationByApplicationIdAsync(int applicationId)
    {
        return await _repository.GetByApplicationIdAsync(applicationId);
    }

    public async Task<string> GetApplicationUrlAsync(int applicationId)
    {
        var installation = await _repository.GetByApplicationIdAsync(applicationId);

        if (installation == null)
        {
            throw new InvalidOperationException($"Installation with ApplicationId '{applicationId}' not found in database");
        }

        return installation.FullUrl;
    }

    public async Task<IEnumerable<Installation>> GetAllInstallationsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Installation> CreateInstallationAsync(Installation installation)
    {
        return await _repository.AddAsync(installation);
    }

    public async Task<Installation> UpdateInstallationAsync(Installation installation)
    {
        return await _repository.UpdateAsync(installation);
    }

    public async Task<bool> DeleteInstallationAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }
}
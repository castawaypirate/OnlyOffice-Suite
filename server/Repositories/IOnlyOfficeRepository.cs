using OnlyOfficeServer.Models;

namespace OnlyOfficeServer.Repositories;

public interface IOnlyOfficeRepository : IDisposable
{
    Task<FileEntity?> GetFileByIdAsync(int id);
}
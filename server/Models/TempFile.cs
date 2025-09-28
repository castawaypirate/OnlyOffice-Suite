namespace OnlyOfficeServer.Models;

public class TempFile
{
    public string Id { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string TempFilePath { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
using System.ComponentModel.DataAnnotations;

namespace OnlyOfficeServer.Models;

public class CallbackRequest
{
    [Required]
    public string Key { get; set; } = string.Empty;

    [Required]
    public int Status { get; set; }

    public string? Url { get; set; }

    public string[]? Users { get; set; }

    public object[]? Actions { get; set; }

    public DateTime? LastSave { get; set; }

    public string? FormsDataUrl { get; set; }
}

public class CallbackResponse
{
    public int Error { get; set; }
    public string? Message { get; set; }
}

public class ForceSaveRequest
{
    [Required]
    public string Key { get; set; } = string.Empty;

    public string? Source { get; set; }  // "save-and-close" or null
}
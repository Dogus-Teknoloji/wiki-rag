using System.ComponentModel.DataAnnotations;

namespace WikiRAG.Models.DTOs;

public class DocumentRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(50000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Author { get; set; }

    public List<string>? Tags { get; set; }
}

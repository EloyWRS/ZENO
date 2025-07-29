using System.ComponentModel.DataAnnotations;

namespace ZENO_API_II.DTOs.Audio;

public class AudioUploadDto
{
    [Required]
    public Guid ThreadId { get; set; }

    [Required]
    public IFormFile AudioFile { get; set; }
}
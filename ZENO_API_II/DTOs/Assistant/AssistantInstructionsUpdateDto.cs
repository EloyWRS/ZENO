using System.ComponentModel.DataAnnotations;

namespace ZENO_API_II.DTOs.Assistant;

public class AssistantInstructionsUpdateDto
{
    [Required]
    public string Instructions { get; set; }
}



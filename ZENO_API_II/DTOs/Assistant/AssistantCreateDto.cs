
using System.ComponentModel.DataAnnotations;

namespace ZENO_API_II.DTOs.Assistant;

    public class AssistantCreateDto
    {
        [Required]
        public string Name { get; set; } = "Zeno";

        public string? Description { get; set; }
    }


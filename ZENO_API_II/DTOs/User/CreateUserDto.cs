
using System.ComponentModel.DataAnnotations;

namespace ZENO_API_II.DTOs.User
{
    public class CreateUserDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string Language { get; set; } = "pt-PT";
    }
}


using System.ComponentModel.DataAnnotations;

namespace ZENO_API_II.DTOs.User
{
    public class UpdateUserDto
    {
        [MaxLength(100)]
        public string? Name { get; set; }

        public string? Language { get; set; }
    }
}

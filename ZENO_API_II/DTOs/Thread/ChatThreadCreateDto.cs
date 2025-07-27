
using System.ComponentModel.DataAnnotations;

namespace Zeno_API_II.DTOs.Thread
{
    public class ChatThreadCreateDto
    {
        //[Required]
        [MaxLength(200)]
        public string Title { get; set; }
    }
}

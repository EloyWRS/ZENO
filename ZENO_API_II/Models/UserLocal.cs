using OpenAI.Assistants;
using System.ComponentModel.DataAnnotations;

namespace ZENO_API_II.Models
{

    using System;
    using System.ComponentModel.DataAnnotations;

    public class UserLocal
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string Language { get; set; } = "pt-PT";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public AssistantLocal Assistant { get; set; } // 1:1
        public int Credits { get; set; } = 0;
        public DateTime LastCreditUpdate { get; set; } = DateTime.UtcNow;

        public List<CreditTransaction> CreditTransactions { get; set; } = new();
    }


}

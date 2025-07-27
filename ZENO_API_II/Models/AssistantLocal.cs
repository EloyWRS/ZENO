using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;

namespace ZENO_API_II.Models;    

    public class AssistantLocal
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; } = "Zeno";

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid UserLocalId { get; set; }

        public UserLocal User { get; set; }

        public ICollection<ChatThread> Threads { get; set; }
        public string? OpenAI_Id { get; set; } // ID real da OpenAI

}



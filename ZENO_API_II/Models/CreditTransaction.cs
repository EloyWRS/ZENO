namespace ZENO_API_II.Models;

    public class CreditTransaction
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public UserLocal User { get; set; }
        public int Amount { get; set; } // Negativo = uso, Positivo = compra
        public string Description { get; set; } // "Mensagem enviada", "Pacote X", etc.
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }


namespace ZENO_API_II.DTOs.CreditTransaction;
public class CreditTransactionCreateDto
{
    public Guid UserId { get; set; } // Pode ser opcional se fores buscar pelo token ou contexto
    public int Amount { get; set; }  // Negativo para consumo, positivo para compra
    public string Description { get; set; }
}

namespace ZENO_API_II.DTOs.CreditTransaction;
public class CreditTransactionReadDto
{
    public Guid Id { get; set; }
    public int Amount { get; set; }
    public string Description { get; set; }
    public DateTime Timestamp { get; set; }
}

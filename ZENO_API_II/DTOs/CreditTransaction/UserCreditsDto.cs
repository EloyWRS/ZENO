namespace ZENO_API_II.DTOs.CreditTransaction;

public class UserCreditsDto
{
    public int Credits { get; set; }
    public List<CreditTransactionReadDto> Transactions { get; set; }
}


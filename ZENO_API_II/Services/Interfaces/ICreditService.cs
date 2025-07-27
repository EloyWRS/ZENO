using ZENO_API_II.DTOs.CreditTransaction;

namespace ZENO_API_II.Services.Interfaces;
public interface ICreditService
{
    Task<bool> HasEnoughCredits(Guid userId, int required);
    Task<bool> ConsumeCredits(Guid userId, int amount, string reason);
    Task<UserCreditsDto?> GetUserCreditsAsync(Guid userId);
    Task<bool> AddCreditsAsync(CreditTransactionCreateDto dto);
}


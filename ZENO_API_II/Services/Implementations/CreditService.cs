using Microsoft.EntityFrameworkCore;
using System;
using ZENO_API_II.Data;
using ZENO_API_II.DTOs.CreditTransaction;
using ZENO_API_II.Models;
using ZENO_API_II.Services.Interfaces;

namespace ZENO_API_II.Services.Implementations;

public class CreditService : ICreditService
{
    private readonly ZenoDbContext _context;

    public CreditService(ZenoDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasEnoughCredits(Guid userId, int required)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null && user.Credits >= required;
    }

    public async Task<bool> ConsumeCredits(Guid userId, int amount, string reason)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.Credits < amount)
            return false;

        user.Credits -= amount;

        _context.CreditTransactions.Add(new CreditTransaction
        {
            UserId = userId,
            Amount = -amount,
            Description = reason
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserCreditsDto?> GetUserCreditsAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.CreditTransactions)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        return new UserCreditsDto
        {
            Credits = user.Credits,
            Transactions = user.CreditTransactions
                .OrderByDescending(t => t.Timestamp)
                .Select(t => new CreditTransactionReadDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Description = t.Description,
                    Timestamp = t.Timestamp
                }).ToList()
        };
    }

    public async Task<bool> AddCreditsAsync(CreditTransactionCreateDto dto)
    {
        var user = await _context.Users.FindAsync(dto.UserId);
        if (user == null) return false;

        user.Credits += dto.Amount;

        _context.CreditTransactions.Add(new CreditTransaction
        {
            UserId = dto.UserId,
            Amount = dto.Amount,
            Description = dto.Description
        });

        await _context.SaveChangesAsync();
        return true;
    }
}



using GenerateData.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenerateData;

public static class Program
{
    public static async Task Main()
    {
        await using var crmContext = new CrmContext();
        var accounts = await crmContext.Accounts.ToListAsync();
        
        await using var tSContext = new TransactionStoreContext();

        int count = 100;
        foreach (var account in accounts)
        {
            for (int i = 0; i < 8; i++)
            {
                Random random = new Random();
                DateTime startDateTime = new DateTime(2023, 1, 1, 0, 0, 0, 0); // 1 января 2023, 00:00:00
                DateTime endDateTime = new DateTime(2024, 1, 1, 23, 59, 59, 59); // 1 января 2024, 23:59:59
                TransactionDto deposit = new TransactionDto()
                {
                    AccountId = account.Id,
                    Amount = random.Next(1, 1000000),
                    TransactionType = TransactionType.Deposit,
                    Date = GetRandomDateTime(startDateTime, endDateTime)
                };
                await tSContext.Transactions.AddAsync(deposit);
            }
            
            for (int i = 0; i < 4; i++)
            {
                Random random = new Random();
                DateTime startDateTime = new DateTime(2023, 1, 1, 0, 0, 0, 0); // 1 января 2023, 00:00:00
                DateTime endDateTime = new DateTime(2024, 1, 1, 23, 59, 59, 59); // 1 января 2024, 23:59:59
                TransactionDto withdraw = new TransactionDto()
                {
                    AccountId = account.Id,
                    Amount = random.Next(1, 1000000) * -1,
                    TransactionType = TransactionType.Withdraw,
                    Date = GetRandomDateTime(startDateTime, endDateTime)
                };
                await tSContext.Transactions.AddAsync(withdraw);
            }
            
            for (int i = 0; i < 28; i++)
            {
                Random random = new Random();
                DateTime startDateTime = new DateTime(2023, 1, 1, 0, 0, 0, 0); // 1 января 2023, 00:00:00
                DateTime endDateTime = new DateTime(2024, 1, 1, 23, 59, 59, 59); // 1 января 2024, 23:59:59
                
                var date = GetRandomDateTime(startDateTime, endDateTime);
                
                TransactionDto transferWithdraw = new TransactionDto()
                {
                    AccountId = account.Id,
                    Amount = random.Next(1, 1000000) * -1,
                    TransactionType = TransactionType.Transfer,
                    Date = date
                };    
                
                TransactionDto transferDeposit = new TransactionDto()
                {
                    AccountId = account.Id,
                    Amount = random.Next(1, 1000000),
                    TransactionType = TransactionType.Transfer,
                    Date = date
                };
                
                await tSContext.Transactions.AddAsync(transferWithdraw);
                await tSContext.Transactions.AddAsync(transferDeposit);
            }

            count++;

            if (count >= 100) break;
        }
        await tSContext.SaveChangesAsync();
        //var transactions = await tSContext.Transactions.ToListAsync();
        Console.WriteLine();
    }
    
    public static DateTime GetRandomDateTime(DateTime startDate, DateTime endDate)
    {
        Random random = new Random();
        int range = (endDate - startDate).Days;
        DateTime randomDate = startDate.AddDays(random.Next(range));
        return DateTime.SpecifyKind(randomDate, DateTimeKind.Utc);
    }
}
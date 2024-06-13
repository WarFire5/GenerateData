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

        int count = 0;
        foreach (var account in accounts)
        {
            // Добавление депозитных транзакций
            for (int i = 0; i < 8; i++)
            {
                DateTime startDateTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime endDateTime = DateTime.UtcNow;
                DateTime date = GetRandomDateTime(startDateTime, endDateTime);

                Random random = new Random();
                TransactionDto deposit = new TransactionDto()
                {
                    AccountId = account.Id,
                    Amount = random.Next(1, 1000000),
                    TransactionType = TransactionType.Deposit,
                    Date = date
                };
                await tSContext.Transactions.AddAsync(deposit);
            }

            // Добавление withdrawal транзакций
            for (int i = 0; i < 4; i++)
            {
                DateTime startDateTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime endDateTime = DateTime.UtcNow;
                DateTime date = GetRandomDateTime(startDateTime, endDateTime);

                Random random = new Random();
                TransactionDto withdraw = new TransactionDto()
                {
                    AccountId = account.Id,
                    Amount = random.Next(1, 1000000) * -1,
                    TransactionType = TransactionType.Withdraw,
                    Date = date
                };
                await tSContext.Transactions.AddAsync(withdraw);
            }

            // Добавление transfer транзакций
            for (int i = 0; i < 28; i++)
            {
                DateTime startDateTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime endDateTime = DateTime.UtcNow;
                DateTime date = GetRandomDateTime(startDateTime, endDateTime);

                Random random = new Random();
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
        Console.WriteLine("Transactions added successfully.");
    }

    public static DateTime GetRandomDateTime(DateTime startDate, DateTime endDate)
    {
        Random random = new Random();
        int range = (endDate - startDate).Days;
        DateTime randomDate = startDate.AddDays(random.Next(range));

        // Генерация случайного времени
        randomDate = randomDate.Date // Убираем часть с временем
            .AddHours(random.Next(0, 24)) // Генерируем часы от 0 до 23
            .AddMinutes(random.Next(0, 60)) // Генерируем минуты от 0 до 59
            .AddSeconds(random.Next(0, 60)) // Генерируем секунды от 0 до 59
            .AddMilliseconds(random.Next(0, 1000)); // Генерируем миллисекунды от 0 до 999

        return randomDate;
    }
}
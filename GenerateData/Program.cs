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
        Random random = new Random(); // Чтобы избежать создания одинаковых значений

        foreach (var account in accounts)
        {
            // Добавление депозитных транзакций
            for (int i = 0; i < 2; i++)
            {
                DateTime startDateTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime endDateTime = DateTime.UtcNow;
                DateTime date = GetRandomDateTime(startDateTime, endDateTime);

                TransactionDto deposit = new TransactionDto()
                {
                    AccountId = account.Id,
                    Amount = GetRandomWholeAmount(random),
                    TransactionType = TransactionType.Deposit,
                    Date = date
                };
                await tSContext.Transactions.AddAsync(deposit);
            }

            // Добавление withdrawal транзакций
            for (int i = 0; i < 1; i++)
            {
                DateTime startDateTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime endDateTime = DateTime.UtcNow;
                DateTime date = GetRandomDateTime(startDateTime, endDateTime);

                TransactionDto withdraw = new TransactionDto()
                {
                    AccountId = account.Id,
                    Amount = GetRandomWholeAmount(random) * -1,
                    TransactionType = TransactionType.Withdraw,
                    Date = date
                };
                await tSContext.Transactions.AddAsync(withdraw);
            }

            // Добавление transfer транзакций
            for (int i = 0; i < 4; i++)
            {
                DateTime startDateTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                DateTime endDateTime = DateTime.UtcNow;
                DateTime date = GetRandomDateTime(startDateTime, endDateTime);

                TransactionDto transferWithdraw = new TransactionDto()
                {
                    AccountId = account.Id,
                    Amount = GetRandomWholeAmount(random) * -1,
                    TransactionType = TransactionType.Transfer,
                    Date = date
                };

                TransactionDto transferDeposit = new TransactionDto()
                {
                    AccountId = account.Id,
                    Amount = GetRandomNonZeroFractionalAmount(random),
                    TransactionType = TransactionType.Transfer,
                    Date = date
                };

                await tSContext.Transactions.AddAsync(transferWithdraw);
                await tSContext.Transactions.AddAsync(transferDeposit);
            }

            count++;

            if (count >= 3818182) break;
        }

        await tSContext.SaveChangesAsync();
        Console.WriteLine("Transactions added successfully.");
    }

    public static DateTime GetRandomDateTime(DateTime startDate, DateTime endDate)
    {
        Random random = new Random();
        int range = (endDate - startDate).Days;
        int randomDays = random.Next(range);
        int randomMilliseconds = random.Next(0, 86400000); // 86400000 миллисекунд = 24 часа

        DateTime randomDate = startDate.AddDays(randomDays).AddMilliseconds(randomMilliseconds);

        return randomDate;
    }

    public static decimal GetRandomWholeAmount(Random random)
    {
        return random.Next(1, 1000000);
    }

    public static decimal GetRandomNonZeroFractionalAmount(Random random)
    {
        int integerPart = random.Next(1, 1000000);
        double fractionalPart;
        do
        {
            fractionalPart = random.NextDouble(); // Случайное число от 0.0 до 1.0
        } while (Math.Abs(fractionalPart) < 1e-6); // Повторяем, пока дробная часть не станет ненулевой

        decimal amount = (decimal)(integerPart + fractionalPart);
        return Math.Round(amount, 4); // Округление до 4 знаков после запятой
    }
}
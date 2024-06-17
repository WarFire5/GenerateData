using GenerateData.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenerateData;

public static class Program
{
    public static async Task Main()
    {
        await using var crmContext = new CrmContext();
        var accounts = await crmContext.Accounts.Select(t => t.Id).ToListAsync();
        Console.WriteLine("Подключение к CRM контексту и получение аккаунтов.");

        int totalTransactions = 30002358;

        int totalDepositTransactions = (int)Math.Ceiling(totalTransactions * 0.2); // 20% на Ввод
        int totalWithdrawTransactions = (int)Math.Floor(totalTransactions * 0.1); // 10% на Вывод
        int totalTransferTransactions = totalTransactions - totalDepositTransactions - totalWithdrawTransactions; // Оставшиеся 70% на Трансфер

        if (totalTransferTransactions % 2 != 0)
        {
            totalTransferTransactions--; // Уменьшаем на 1, чтобы сделать четным
            totalDepositTransactions++; // Компенсируем добавлением 1 к Вводам
        }

        await using var tSContext = new TransactionStoreContext();
        Random random = new Random();
        Console.WriteLine("Подключение к TransactionStore контексту для добавления транзакций.");

        await AddTransactions(tSContext, accounts, totalWithdrawTransactions, totalDepositTransactions, totalTransferTransactions, random);
        Console.WriteLine("Транзакции успешно добавлены.");
    }

    public static async Task AddTransactions(TransactionStoreContext context, List<Guid> accountIds, int withdrawCount, int depositCount, int transferCount, Random random)
    {
        Console.WriteLine("Генерируем deposit");
        await AddRandomTransactionsWithRetry(context, accountIds, depositCount, TransactionType.Deposit, random);
        Console.WriteLine("Генерируем withdraw");
        await AddRandomTransactionsWithRetry(context, accountIds, withdrawCount, TransactionType.Withdraw, random);
        Console.WriteLine("Генерируем transfer");
        await AddRandomTransferTransactionsWithRetry(context, accountIds, transferCount / 2, random); // Делим на 2, т.к. каждая трансферная транзакция состоит из двух частей
    }

    public static async Task AddRandomTransactionsWithRetry(TransactionStoreContext context, List<Guid> accountIds, int batchSize, TransactionType type, Random random)
    {
        DateTime startDateTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime endDateTime = DateTime.UtcNow;

        int totalTransactionsToAdd = batchSize;
        int index = 0;

        while (totalTransactionsToAdd > 0)
        {
            List<TransactionDto> transactions = new List<TransactionDto>();
            int currentBatchSize = Math.Min(300000, totalTransactionsToAdd); // Устанавливаем размер текущего пакета

            for (int i = 0; i < currentBatchSize && index < accountIds.Count; i++)
            {
                DateTime date = GetRandomDateTime(startDateTime, endDateTime);
                Guid accountId = accountIds[index];

                TransactionDto transaction = new TransactionDto()
                {
                    AccountId = accountId,
                    Amount = type == TransactionType.Withdraw
                        ? -GetRandomNonZeroFractionalAmount(random)
                        : GetRandomNonZeroFractionalAmount(random),
                    TransactionType = type,
                    Date = date
                };

                transactions.Add(transaction);
                index++;
            }

            await InsertTransactionsWithRetry(context, transactions);
            totalTransactionsToAdd -= currentBatchSize;
        }
    }

    public static async Task AddRandomTransferTransactionsWithRetry(TransactionStoreContext context, List<Guid> accountIds, int batchSize, Random random)
    {
        DateTime startDateTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime endDateTime = DateTime.UtcNow;

        int totalTransactionsToAdd = batchSize * 2; // Умножаем на 2, т.к. каждая трансферная транзакция состоит из двух частей
        int index = 0;

        while (totalTransactionsToAdd > 0)
        {
            List<TransactionDto> transactions = new List<TransactionDto>();
            int currentBatchSize = Math.Min(300000, totalTransactionsToAdd); // Устанавливаем размер текущего пакета

            for (int i = 0; i < currentBatchSize && index < accountIds.Count - 1; i++) // Учитываем -1, чтобы иметь достаточно аккаунтов для парных операций
            {
                DateTime date = GetRandomDateTime(startDateTime, endDateTime);
                Guid withdrawAccountId = accountIds[index];
                Guid depositAccountId;

                // Получаем другой случайный аккаунт для депозита, отличный от аккаунта для вывода
                do
                {
                    depositAccountId = accountIds[random.Next(accountIds.Count)];
                }
                while (depositAccountId == withdrawAccountId);

                decimal withdrawAmount = GetRandomNonZeroFractionalAmount(random) * -1; // Отрицательное значение для вывода
                decimal depositAmount = GetRandomNonZeroFractionalAmount(random); // Положительное значение для депозита

                TransactionDto transferWithdraw = new TransactionDto()
                {
                    AccountId = withdrawAccountId,
                    Amount = withdrawAmount,
                    TransactionType = TransactionType.Transfer,
                    Date = date
                };

                TransactionDto transferDeposit = new TransactionDto()
                {
                    AccountId = depositAccountId,
                    Amount = depositAmount,
                    TransactionType = TransactionType.Transfer,
                    Date = date
                };

                transactions.Add(transferWithdraw);
                transactions.Add(transferDeposit);
                index++;
            }

            await InsertTransactionsWithRetry(context, transactions);
            totalTransactionsToAdd -= currentBatchSize;
        }
    }

    public static async Task InsertTransactionsWithRetry(TransactionStoreContext context, List<TransactionDto> transactions)
    {
        const int batchSize = 300000;
        const int maxRetries = 10;
        int retryCount = 0;
        bool success = false;

        while (!success && retryCount < maxRetries)
        {
            for (int i = 0; i < transactions.Count; i += batchSize)
            {
                var batch = transactions.Skip(i).Take(batchSize).ToList();
                try
                {
                    await context.Transactions.AddRangeAsync(batch);
                    await context.SaveChangesAsync();
                    context.ChangeTracker.Clear(); // Очищение контекста между вставками
                    success = true;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Console.WriteLine($"Ошибка при вставке транзакций, попытка {retryCount}/{maxRetries}. Ошибка: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);

                    if (retryCount >= maxRetries)
                    {
                        Console.WriteLine("Достигнуто максимальное количество повторов. Вставка не удалась.");
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromMinutes(1)); // Ожидание перед повторной попыткой
                }
            }
        }
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

    public static decimal GetRandomNonZeroFractionalAmount(Random random)
    {
        int integerPart = random.Next(1, 1000000);
        double fractionalPart;
        do
        {
            fractionalPart = random.NextDouble(); // Случайное число от 0.0 до 1.0
        }
        while (Math.Abs(fractionalPart) < 1e-6); // Повторяем, пока дробная часть не станет ненулевой

        decimal amount = (decimal)(integerPart + fractionalPart);
        return Math.Round(amount, 4); // Округление до 4 знаков после запятой
    }
}
using EFCore.BulkExtensions;
using GenerateData.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenerateData;

public static class Program
{
    public static async Task Main()
    {
        await using var crmContext = new CrmContext();
        var accounts = await crmContext.Accounts.Select(t => t.Id).ToListAsync();
        Console.WriteLine("Подключение к CRM контексту и получение первых 3000236 аккаунтов.");

        int totalTransactions = 30002358;

        int totalTransferTransactions = (int)Math.Ceiling(totalTransactions * 0.7); // 70% на Трансфер
        int totalWithdrawTransactions = (int)Math.Floor(totalTransactions * 0.10); // 10% на Вывод
        int totalDepositTransactions = totalTransactions - totalTransferTransactions - totalWithdrawTransactions; // Оставшиеся 20% на Ввод

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
        // Распределение транзакций по типам (Ввод, Вывод, Трансфер)
        await AddRandomTransactionsWithRetry(context, accountIds, depositCount, TransactionType.Deposit, random);
        await AddRandomTransactionsWithRetry(context, accountIds, withdrawCount, TransactionType.Withdraw, random);
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
            List<TransactionDto> transactions = [];

            for (int i = 0; i < batchSize && index < accountIds.Count; i++)
            {
                DateTime date = GetRandomDateTime(startDateTime, endDateTime);
                Guid accountId = accountIds[index];

                TransactionDto transaction = new TransactionDto()
                {
                    Id = Guid.NewGuid(), // Генерация уникального идентификатора
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

            await BulkInsertWithRetry(context, transactions);
            totalTransactionsToAdd -= batchSize;
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
            List<TransactionDto> transactions = [];

            for (int i = 0; i < batchSize && index < accountIds.Count; i++)
            {
                DateTime date = GetRandomDateTime(startDateTime, endDateTime);
                Guid accountId = accountIds[index];

                decimal amount = GetRandomNonZeroFractionalAmount(random);

                TransactionDto transferWithdraw = new TransactionDto()
                {
                    Id = Guid.NewGuid(), // Генерация уникального идентификатора
                    AccountId = accountId,
                    Amount = -amount,
                    TransactionType = TransactionType.Transfer,
                    Date = date
                };

                TransactionDto transferDeposit = new TransactionDto()
                {
                    Id = Guid.NewGuid(), // Генерация уникального идентификатора
                    AccountId = accountId,
                    Amount = amount,
                    TransactionType = TransactionType.Transfer,
                    Date = date
                };

                transactions.Add(transferWithdraw);
                transactions.Add(transferDeposit);
                index++;
            }

            await BulkInsertWithRetry(context, transactions);
            totalTransactionsToAdd -= batchSize * 2;
        }
    }

    public static async Task BulkInsertWithRetry(TransactionStoreContext context, List<TransactionDto> transactions)
    {
        const int maxRetries = 10;
        int retryCount = 0;
        bool success = false;

        while (!success && retryCount < maxRetries)
        {
            try
            {
                await context.BulkInsertAsync(transactions);
                context.ChangeTracker.Clear(); // Очищение контекста между вставками
                success = true;
            }
            catch (Exception ex)
            {
                retryCount++;
                Console.WriteLine($"Ошибка при пакетной вставке, повтор {retryCount}/{maxRetries}. Ошибка: {ex.Message}");

                if (retryCount >= maxRetries)
                {
                    Console.WriteLine("Достигнуто максимальное количество повторов. Вставка не удалась.");
                    throw;
                }

                Console.WriteLine("Дополнительная информация для отладки:"); // Логирование дополнительных данных о проблеме, если необходимо
                Console.WriteLine(ex.StackTrace);
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
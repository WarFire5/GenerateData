using GenerateData.Enums;

namespace GenerateData;

public class TransactionDto: IdContainer
{
    public Guid AccountId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}
using EventFlow.Attributes;

namespace ClearFlow.ValueObjects.Dtos;
public class MoneyDto
{
    [StringDisplayValue("GBP")]
    public string Currency { get; }
    [DoubleDisplayValue(400.0)]
    public decimal Amount { get; }
    public MoneyDto(string currency, decimal amount)
    {
        Currency = currency;
        Amount = amount;
    }
}

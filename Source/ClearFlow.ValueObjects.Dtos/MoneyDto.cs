using EventFlow.Attributes;

namespace ClearFlow.ValueObjects.Dtos;
public class MoneyDto
{
    [ExampleStringDisplayValue("GBP")]
    public string Currency { get; }
    [ExampleDoubleDisplayValue(400.0)]
    public decimal Amount { get; }
    public MoneyDto(string currency, decimal amount)
    {
        Currency = currency;
        Amount = amount;
    }
}

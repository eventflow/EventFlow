namespace EventFlow.Attributes;

public class AggregateIdDisplayValueAttribute : DisplayValueAttribute
{
    private const string DefaultExample = "[AggregateName]-[GUID]";
    public AggregateIdDisplayValueAttribute() : base(DefaultExample)
    {
    }
}

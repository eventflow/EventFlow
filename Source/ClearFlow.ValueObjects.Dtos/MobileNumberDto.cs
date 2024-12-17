using EventFlow.Attributes;

namespace ClearFlow.ValueObjects.Dtos;
public class MobileNumberDto
{
    [StringDisplayValue("US")]
    public string CountryCode { get; }
    [StringDisplayValue("+14156667777")]
    public string MobileNumber { get; }
    public MobileNumberDto(string countryCode, string mobileNumber)
    {
        CountryCode = countryCode;
        MobileNumber = mobileNumber;
    }
}
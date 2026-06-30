namespace MHStore.Services.AddressService;

public class AddressSuggestionResponse
{
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
}

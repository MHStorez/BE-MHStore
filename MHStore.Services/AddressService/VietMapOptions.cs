namespace MHStore.Services.AddressService;

public class VietMapOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string AutocompleteUrl { get; set; } = "https://maps.vietmap.vn/api/autocomplete/v3";
    public string ReverseUrl { get; set; } = "https://maps.vietmap.vn/api/reverse/v3";
}

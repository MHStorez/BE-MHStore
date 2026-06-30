namespace MHStore.Services.AddressService;

public interface IService
{
    Task<IEnumerable<AddressSuggestionResponse>> AutocompleteAsync(string query);
    Task<AddressSuggestionResponse> ReverseAsync(decimal latitude, decimal longitude);
}

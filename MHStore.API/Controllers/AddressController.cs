using Microsoft.AspNetCore.Mvc;
using AddressService = MHStore.Services.AddressService.IService;
using AddressSuggestionResponse = MHStore.Services.AddressService.AddressSuggestionResponse;

namespace MHStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AddressController : ControllerBase
{
    private readonly AddressService _addressService;

    public AddressController(AddressService addressService)
    {
        _addressService = addressService;
    }

    [HttpGet("autocomplete")]
    public async Task<ActionResult<IEnumerable<AddressSuggestionResponse>>> Autocomplete([FromQuery] string query)
    {
        try
        {
            var suggestions = await _addressService.AutocompleteAsync(query);

            return Ok(suggestions);
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, exception.Message);
        }
        catch (HttpRequestException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, exception.Message);
        }
    }

    [HttpGet("reverse")]
    public async Task<ActionResult<AddressSuggestionResponse>> Reverse([FromQuery] decimal latitude, [FromQuery] decimal longitude)
    {
        try
        {
            var address = await _addressService.ReverseAsync(latitude, longitude);

            return Ok(address);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, exception.Message);
        }
        catch (HttpRequestException exception)
        {
            return StatusCode(StatusCodes.Status502BadGateway, exception.Message);
        }
    }
}

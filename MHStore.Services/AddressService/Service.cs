using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MHStore.Services.AddressService;

public class Service : IService
{
    private readonly HttpClient _httpClient;
    private readonly VietMapOptions _options;

    public Service(HttpClient httpClient, IOptions<VietMapOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<IEnumerable<AddressSuggestionResponse>> AutocompleteAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return [];
        }

        EnsureConfigured();
        var url = BuildUrl(_options.AutocompleteUrl, [
            ("apikey", _options.ApiKey),
            ("text", query.Trim())
        ]);
        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        return ReadItems(document.RootElement)
            .Select(ToSuggestion)
            .Where(suggestion => !string.IsNullOrWhiteSpace(suggestion.Address))
            .Take(8)
            .ToList();
    }

    public async Task<AddressSuggestionResponse> ReverseAsync(decimal latitude, decimal longitude)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentException("Latitude is invalid.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentException("Longitude is invalid.");
        }

        EnsureConfigured();
        var url = BuildUrl(_options.ReverseUrl, [
            ("apikey", _options.ApiKey),
            ("lat", latitude.ToString(CultureInfo.InvariantCulture)),
            ("lng", longitude.ToString(CultureInfo.InvariantCulture))
        ]);
        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);
        var item = ReadItems(document.RootElement).FirstOrDefault();
        var suggestion = item.ValueKind == JsonValueKind.Undefined
            ? ToSuggestion(document.RootElement)
            : ToSuggestion(item);

        suggestion.Latitude ??= latitude;
        suggestion.Longitude ??= longitude;

        return suggestion;
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("VietMap API key is not configured.");
        }
    }

    private static string BuildUrl(string baseUrl, IEnumerable<(string Key, string Value)> query)
    {
        var separator = baseUrl.Contains('?') ? '&' : '?';
        var queryString = string.Join("&", query.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

        return $"{baseUrl}{separator}{queryString}";
    }

    private static IEnumerable<JsonElement> ReadItems(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root.EnumerateArray();
        }

        foreach (var propertyName in new[] { "data", "items", "results", "features" })
        {
            if (root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Array)
            {
                return property.EnumerateArray();
            }
        }

        return root.ValueKind == JsonValueKind.Object ? [root] : [];
    }

    private static AddressSuggestionResponse ToSuggestion(JsonElement element)
    {
        var address = ReadString(element, "display", "address", "name", "formatted_address", "label");
        var referenceId = ReadString(element, "ref_id", "refId", "reference_id", "place_id", "id");
        var latitude = ReadDecimal(element, "lat", "latitude");
        var longitude = ReadDecimal(element, "lng", "lon", "longitude");

        if ((!latitude.HasValue || !longitude.HasValue) &&
            element.TryGetProperty("geometry", out var geometry) &&
            geometry.ValueKind == JsonValueKind.Object)
        {
            latitude ??= ReadDecimal(geometry, "lat", "latitude");
            longitude ??= ReadDecimal(geometry, "lng", "lon", "longitude");

            if (geometry.TryGetProperty("coordinates", out var coordinates) &&
                coordinates.ValueKind == JsonValueKind.Array)
            {
                var values = coordinates.EnumerateArray().ToArray();
                if (values.Length >= 2)
                {
                    longitude ??= ToDecimal(values[0]);
                    latitude ??= ToDecimal(values[1]);
                }
            }
        }

        return new AddressSuggestionResponse
        {
            Address = address,
            Latitude = latitude,
            Longitude = longitude,
            ReferenceId = referenceId
        };
    }

    private static string ReadString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var property))
            {
                return property.ValueKind switch
                {
                    JsonValueKind.String => property.GetString() ?? string.Empty,
                    JsonValueKind.Number => property.GetRawText(),
                    _ => string.Empty
                };
            }
        }

        return string.Empty;
    }

    private static decimal? ReadDecimal(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var property))
            {
                var value = ToDecimal(property);
                if (value.HasValue)
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static decimal? ToDecimal(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var number))
        {
            return number;
        }

        if (element.ValueKind == JsonValueKind.String &&
            decimal.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}

using System.Text.Json.Serialization;

namespace EcommerceFrontend.Models;

public class AuthUser
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("rol")]
    public string Rol { get; set; } = string.Empty;

    [JsonPropertyName("reto")]
    public string? Reto { get; set; }

    [JsonPropertyName("jwt")]
    public string Jwt { get; set; } = string.Empty;
}
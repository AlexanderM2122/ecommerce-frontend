using System.Text.Json.Serialization;

namespace EcommerceFrontend.Models;

public sealed class RegistroBitacora
{
    [JsonPropertyName("bitacoraId")]
    public int Id { get; init; }

    public string Accion { get; init; } = string.Empty;

    [JsonPropertyName("elementoid")]
    public string? ElementoId { get; init; }

    public string Ip { get; init; } = string.Empty;
    public string Usuario { get; init; } = string.Empty;
    public DateTimeOffset? Fecha { get; init; }
}

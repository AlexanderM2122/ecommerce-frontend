
using System.Text.Json.Serialization;

namespace EcommerceFrontend.Models;

public class Producto
{
    [JsonPropertyName("productoId")]
    public int ProductoId { get; set; }

    [JsonPropertyName("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [JsonPropertyName("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("precio")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public decimal Precio { get; set; }

    [JsonPropertyName("archivoid")]
    public int? ArchivoId { get; set; }

    [JsonPropertyName("categorias")]
    public List<Categoria> Categorias { get; set; } = [];
}

namespace EcommerceFrontend.Models;

public sealed class Carrito
{
    public List<CarritoItem> Items { get; init; } = [];
    public decimal Total { get; init; }
}

public sealed class CarritoItem
{
    public int ProductoId { get; init; }
    public string Titulo { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public int Cantidad { get; init; }
    public decimal Subtotal { get; init; }
}

namespace EcommerceFrontend.Models;

public sealed class Pedido
{
    public int Id { get; init; }
    public decimal Total { get; init; }
    public string Estado { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public PedidoUsuario? Usuario { get; init; }
    public List<PedidoDetalle> Detalles { get; init; } = [];
}

public sealed class PedidoUsuario
{
    public string Nombre { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public sealed class PedidoDetalle
{
    public int ProductoId { get; init; }
    public int Cantidad { get; init; }
    public decimal PrecioUnitario { get; init; }
    public PedidoProducto? Producto { get; init; }
}

public sealed class PedidoProducto
{
    public string Titulo { get; init; } = string.Empty;
}

namespace EcommerceFrontend.Models;

public record CatalogoCompra(
    IReadOnlyList<Producto> Productos,
    IReadOnlyList<Categoria> Categorias,
    string? Busqueda,
    int? CategoriaId,
    string? Orden);

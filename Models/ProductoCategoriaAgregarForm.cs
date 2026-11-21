
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EcommerceFrontend.Models;

public class ProductoCategoriaAgregarForm
{
    [Range(1, int.MaxValue,
        ErrorMessage = "Selecciona una categoría.")]
    public int CategoriaId { get; set; }

    [ValidateNever]
    public Producto? Producto { get; set; }

    [ValidateNever]
    public IReadOnlyList<Categoria> CategoriasDisponibles
        { get; set; } = [];
}

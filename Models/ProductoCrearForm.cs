using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EcommerceFrontend.Models;

public class ProductoCrearForm
{
    public const int TituloMinLength = 2;
    public const int TituloMaxLength = 255;
    public const int DescripcionMinLength = 5;
    public const int DescripcionMaxLength = 2000;
    public const string PrecioMin = "0.01";
    public const string PrecioMax = "99999999.99";

    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(TituloMaxLength, MinimumLength = TituloMinLength,
        ErrorMessage = "El título debe tener entre 2 y 255 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(DescripcionMaxLength, MinimumLength = DescripcionMinLength,
        ErrorMessage = "La descripción debe tener entre 5 y 2000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El precio es obligatorio.")]
    [Range(typeof(decimal), PrecioMin, PrecioMax,
        ErrorMessage = "El precio debe estar entre 0.01 y 99,999,999.99.")]
    [RegularExpression(@"^\d+(\.\d{1,2})?$",
        ErrorMessage = "El precio debe ser un número válido con máximo 2 decimales.")]
    public decimal Precio { get; set; }

    public List<int> CategoriaIds { get; set; } = [];

    [ValidateNever]
    public List<Categoria> CategoriasDisponibles { get; set; } = [];


    [ImagenValida]
    public IFormFile? Imagen { get; set; }

    [Range(1, int.MaxValue,
        ErrorMessage = "La imagen seleccionada no es válida.")]
    public int? ArchivoId { get; set; }

    [ValidateNever]
    public List<Archivo> ArchivosDisponibles { get; set; } = [];

}
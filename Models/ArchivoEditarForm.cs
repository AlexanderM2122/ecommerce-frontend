using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EcommerceFrontend.Models;

public class ArchivoEditarForm
{
    [Required(ErrorMessage = "Selecciona una imagen.")]
    [ImagenValida]
    public IFormFile? File { get; set; }

    [ValidateNever]
    public Archivo? Actual { get; set; }
}
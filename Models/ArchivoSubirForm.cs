using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcommerceFrontend.Models;

public class ArchivoSubirForm
{
    [Required(ErrorMessage = "Selecciona una imagen.")]
    [ImagenValida]
    public IFormFile? File { get; set; }
}
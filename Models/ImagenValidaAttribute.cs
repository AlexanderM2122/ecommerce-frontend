using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EcommerceFrontend.Models;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ImagenValidaAttribute(
    int maxMegabytes = ImagenValidaAttribute.TamanoMaximoMegabytesPorDefecto) : ValidationAttribute
{
    public const int BytesPorMegabyte = 1024 * 1024;
    public const int TamanoMaximoMegabytesPorDefecto = 5;

    private static readonly HashSet<string> ExtensionesPermitidas =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg" };

    private static readonly HashSet<string> MimesPermitidos =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg" };

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not IFormFile file)
            return ValidationResult.Success;

        if (file.Length == 0)
            return new ValidationResult("El archivo está vacío.");

        long maxBytes = (long)maxMegabytes * BytesPorMegabyte;
        if (file.Length > maxBytes)
            return new ValidationResult($"La imagen no debe superar los {maxMegabytes} MB.");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !ExtensionesPermitidas.Contains(extension))
            return new ValidationResult("Solamente se permiten imágenes JPG.");

        if (!MimesPermitidos.Contains(file.ContentType))
            return new ValidationResult("El tipo MIME del archivo no corresponde a una imagen JPEG válida.");

        return ValidationResult.Success;
    }
}
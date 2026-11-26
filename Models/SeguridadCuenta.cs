using System.ComponentModel.DataAnnotations;

namespace EcommerceFrontend.Models;

public class CorreoCuentaForm
{
    [Required(ErrorMessage = "Ingresa tu correo.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo v�lido.")]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;
}

public class RestablecerCuentaForm
{
    [Required]
    [RegularExpression("^[A-Za-z0-9_-]{43}$", ErrorMessage = "El enlace no es v�lido.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa una contrase�a nueva.")]
    [StringLength(72, MinimumLength = 8, ErrorMessage = "Usa entre 8 y 72 caracteres.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirma la contrase�a.")]
    [Compare(nameof(Password), ErrorMessage = "Las contrase�as deben coincidir.")]
    [DataType(DataType.Password)]
    public string ConfirmarPassword { get; set; } = string.Empty;
}

public class SegundoFactorForm
{
    [Required]
    [RegularExpression("^[A-Za-z0-9_-]{43}$")]
    public string Reto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa el c�digo.")]
    [RegularExpression(@"^(\d{6}|[a-fA-F0-9]{16})$", ErrorMessage = "Ingresa un c�digo v�lido.")]
    public string Codigo { get; set; } = string.Empty;
}

public record SeguridadCuentaView(bool Activo, string? Secreto = null, string[]? Codigos = null);

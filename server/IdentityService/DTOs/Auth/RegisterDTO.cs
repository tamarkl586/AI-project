using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTOs.Auth;

public class RegisterDTO
{
    [Required(ErrorMessage = "שם הוא שדה חובה")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "טלפון הוא שדה חובה")]
    [Phone(ErrorMessage = "מספר טלפון לא תקין")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "אימייל הוא שדה חובה")]
    [EmailAddress(ErrorMessage = "כתובת אימייל לא תקינה")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "סיסמה היא שדה חובה")]
    [MinLength(6, ErrorMessage = "סיסמה חייבת להיות לפחות 6 תווים")]
    public string Password { get; set; } = string.Empty;
}

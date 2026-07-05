using System.ComponentModel.DataAnnotations;

namespace IdentityService.Models;

public class User
{
    public int Id { get; set; }

    [Required(ErrorMessage = "שדה חובה")]
    [MaxLength(50, ErrorMessage = "שם לא יכול להכיל יותר מ-50 תווים")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "שדה חובה")]
    [Phone(ErrorMessage = "מספר טלפון לא תקין")]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "שדה חובה")]
    [EmailAddress(ErrorMessage = "כתובת אימייל לא תקינה")]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "שדה חובה")]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required(ErrorMessage = "שדה חובה")]
    [MaxLength(20)]
    public string Role { get; set; } = "user";
}

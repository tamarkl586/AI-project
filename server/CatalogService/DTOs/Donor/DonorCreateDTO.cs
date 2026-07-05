using System.ComponentModel.DataAnnotations;

namespace CatalogService.DTOs.Donor
{
    public class DonorCreateDTO
    {
        [Required(ErrorMessage = "תעודת זהות היא שדה חובה")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "תעודת זהות חייבת להכיל 9 ספרות")]
        public string IdentityNumber { get; set; } = null!;

        [Required(ErrorMessage = "שם הוא שדה חובה")]
        [MaxLength(50, ErrorMessage = "שם לא יכול להכיל יותר מ-50 תווים")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "טלפון הוא שדה חובה")]
        [RegularExpression(@"^05\d{8}$", ErrorMessage = "מספר טלפון חייב להתחיל ב-05 ולהכיל 10 ספרות")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "אימייל הוא שדה חובה")]
        [EmailAddress(ErrorMessage = "כתובת אימייל לא תקינה")]
        public string Email { get; set; } = null!;
    }
}

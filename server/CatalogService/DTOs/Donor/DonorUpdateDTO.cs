using System.ComponentModel.DataAnnotations;

namespace CatalogService.DTOs.Donor
{
    public class DonorUpdateDTO
    {
        [MaxLength(50, ErrorMessage = "שם לא יכול להכיל יותר מ-50 תווים")]
        public string? Name { get; set; }

        [RegularExpression(@"^05\d{8}$", ErrorMessage = "מספר טלפון חייב להתחיל ב-05 ולהכיל 10 ספרות")]
        public string? Phone { get; set; }

        [RegularExpression(@"^$|^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", ErrorMessage = "כתובת אימייל לא תקינה")]
        public string? Email { get; set; }
    }
}

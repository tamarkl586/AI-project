using System.ComponentModel.DataAnnotations;

namespace CatalogService.DTOs.Gift
{
    public class CreateGiftDTO
    {
        [Required(ErrorMessage = "שם המתנה הוא שדה חובה")]
        [MaxLength(50, ErrorMessage = "שם לא יכול להיות יותר מ-50 תווים")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "תיאור המתנה הוא שדה חובה")]
        [MaxLength(500, ErrorMessage = "תיאור לא יכול לעלות על 500 תווים")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "חובה להוסיף תמונה")]
        [MaxLength(250, ErrorMessage = "נתיב התמונה ארוך מדי (מקסימום 250 תווים)")]
        public string Picture { get; set; } = null!;

        [Required(ErrorMessage = "מחיר הוא שדה חובה")]
        [Range(5, 500, ErrorMessage = "מחיר חייב להיות בין 5 ל-500 ש\"ח")]
        public int? Price { get; set; }

        [Required(ErrorMessage = "חובה לשייך תורם")]
        public int? DonorId { get; set; }

        [Required(ErrorMessage = "חובה לשייך קטגוריה")]
        public int? CategoryId { get; set; }
    }
}

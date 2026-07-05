using System.ComponentModel.DataAnnotations;

namespace CatalogService.DTOs.Gift
{
    public class GiftUpdateDTO
    {
        [MaxLength(50, ErrorMessage = "שם לא יכול להיות יותר מ-50 תווים")]
        public string? Name { get; set; }

        [MaxLength(500, ErrorMessage = "תיאור לא יכול לעלות על 500 תווים")]
        public string? Description { get; set; }

        [MaxLength(250, ErrorMessage = "נתיב התמונה ארוך מדי")]
        public string? Picture { get; set; }

        [Range(5, 500, ErrorMessage = "מחיר חייב להיות בין 5 ל-500 ש\"ח")]
        public int? Price { get; set; }

        public int? DonorId { get; set; }
        public int? CategoryId { get; set; }
    }
}

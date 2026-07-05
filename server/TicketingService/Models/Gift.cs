using System.ComponentModel.DataAnnotations;

namespace TicketingService.Models
{
    public class Gift
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Picture { get; set; } = string.Empty;

        [Required]
        public int Price { get; set; }

        public int? WinnerId { get; set; }
    }
}

namespace CatalogService.DTOs.Gift
{
    public class GiftDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Picture { get; set; } = string.Empty;
        public int Price { get; set; }
        public int DonorId { get; set; }
        public string DonorName { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>ID of the winning user (from the Identity Service). Null until a draw is run.</summary>
        public int? WinnerId { get; set; }
    }
}

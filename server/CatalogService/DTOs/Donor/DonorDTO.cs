namespace CatalogService.DTOs.Donor
{
    public class DonorDTO
    {
        public int Id { get; set; }
        public string IdentityNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        /// <summary>Populated by DonorService via a secondary query to the gifts collection.</summary>
        public IEnumerable<DTOs.Gift.GiftDTO> Gifts { get; set; } = Enumerable.Empty<DTOs.Gift.GiftDTO>();
    }
}

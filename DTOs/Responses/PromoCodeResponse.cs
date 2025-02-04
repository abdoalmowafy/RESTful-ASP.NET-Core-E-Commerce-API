namespace ECommerceAPI.DTOs.Responses
{
    public class PromoCodeResponse
    {
        public int? Id { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public int? Percent { get; set; }
        public long? MaxSaleCents { get; set; }
        public bool? Active { get; set; }
    }
}

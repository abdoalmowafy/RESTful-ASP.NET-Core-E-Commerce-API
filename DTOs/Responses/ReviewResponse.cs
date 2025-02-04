namespace ECommerceAPI.DTOs.Responses
{
    public class ReviewResponse
    {
        public int? Id { get; set; }
        public UserPriefResponse? Reviewer { get; set; }
        public byte? Rating { get; set; }
        public string? Text { get; set; }
        public DateTime? CreatedDateTime { get; set; }
    }
}

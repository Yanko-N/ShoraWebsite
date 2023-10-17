namespace ShoraWebsite.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public string Description = "Sorry Something went Wrong!\n Don't hate me 1n :(";
    }
}
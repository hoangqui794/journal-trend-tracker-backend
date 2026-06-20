namespace PaperService.DTOs
{
    public class NotificationTriggerDto
    {
        public string Keyword { get; set; } = string.Empty;
        public Guid PaperId { get; set; }
        public string PaperTitle { get; set; } = string.Empty;
    }
}

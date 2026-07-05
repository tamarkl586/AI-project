namespace DrawReportService.DTOs.Report;

public class DrawingResultDTO
{
    public int GiftId { get; set; }
    public string GiftName { get; set; } = string.Empty;
    public int WinnerId { get; set; }
    public string WinnerName { get; set; } = string.Empty;
    public string WinnerEmail { get; set; } = string.Empty;
    public bool EmailSent { get; set; }
    public DateTime DrawnAtUtc { get; set; }
}

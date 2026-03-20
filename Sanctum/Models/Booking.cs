namespace Sanctum.Models;

public class Booking
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Description { get; set; }

    public int UserID { get; set; }

    public User User { get; set; }
}
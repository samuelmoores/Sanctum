namespace Sanctum.Models;

public class Booking
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Description { get; set; }

    public int UserID { get; set; }

    // Added = null! to prevent warnings about non-nullable reference types
    public User User { get; set; } = null!;
}
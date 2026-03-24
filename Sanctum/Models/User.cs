namespace Sanctum.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Description {  get; set; } = null!;
    public int CSULBID { get; set; }

    public ICollection<Booking>? Bookings { get; set; }
}
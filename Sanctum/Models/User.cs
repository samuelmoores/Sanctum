namespace Sanctum.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Description {  get; set; }
    public int CSULBID { get; set; }

    public ICollection<Booking> Bookings { get; set; }
}
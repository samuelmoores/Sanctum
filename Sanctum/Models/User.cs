using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sanctum.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string First { get; set; } = null!;
    public string Last { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Description {  get; set; } = null!;
    public string CSULBID { get; set; } = null!;
    public byte[]? ProfilePhoto { get; set; }
    public string? ProfilePhotoType { get; set; }

    public ICollection<Booking>? Bookings { get; set; }
}
namespace Sanctum.Models
{
    public class Room
    {
        public int Id { get; set; }
        public TimeOnly Time_Limit  { get; set; }
        public int Capacity { get; set; }
        public int Availbility { get; set; }
        public string Building { get; set; }
        public string RoomName { get; set; }
    }
}

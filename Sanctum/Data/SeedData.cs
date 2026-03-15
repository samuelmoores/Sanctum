using Sanctum.Models;

namespace Sanctum.Data;

public class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        var desiredRooms = new List<Room>
        {
            // ECS
            new Room { Building = "COB", RoomName = "COB-201" },
            new Room { Building = "COB", RoomName = "COB-202" },
            new Room { Building = "COB", RoomName = "COB-301" },
            new Room { Building = "COB", RoomName = "COB-302" },
            // LIB
            new Room { Building = "HC", RoomName = "HC-101" },
            new Room { Building = "HC", RoomName = "HC-203" },
            new Room { Building = "HC", RoomName = "HC-305" },
            new Room { Building = "HC", RoomName = "HC-306" },
            // USU
            new Room { Building = "SSSC", RoomName = "SSSC-110" },
            new Room { Building = "SSSC", RoomName = "SSSC-210" },
            // VEC
            new Room { Building = "VEC", RoomName = "VEC-17" },
            new Room { Building = "VEC", RoomName = "VEC-18" },
            new Room { Building = "VEC", RoomName = "VEC-19" },
            new Room { Building = "VEC", RoomName = "VEC-20" }
            
            };

        foreach (var room in desiredRooms)
        {
            if (!context.Rooms.Any(r => r.RoomName == room.RoomName))
            {
                context.Rooms.Add(room); // only add if it doesn't exist yet
            }
        }
        
        context.SaveChanges();
    }
}
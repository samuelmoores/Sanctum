using Sanctum.Models;

namespace Sanctum.Data;

public class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        var desiredRooms = new List<Room>
        {
            // ECS
            new Room { Building = "COB", RoomName = "COB-101" },
            new Room { Building = "COB", RoomName = "COB-102" },
            new Room { Building = "COB", RoomName = "COB-105" },
            new Room { Building = "COB", RoomName = "COB-106" },
            // LIB
            new Room { Building = "HC", RoomName = "HC-101" },
            new Room { Building = "HC", RoomName = "HC-102" },
            new Room { Building = "HC", RoomName = "HC-110" },
            new Room { Building = "HC", RoomName = "HC-120" },
            new Room { Building = "HC", RoomName = "HC-125" },
            // USU
            new Room { Building = "SSSC", RoomName = "SSSC-110" },
            new Room { Building = "SSSC", RoomName = "SSSC-120" },
            // VEC
            new Room { Building = "VEC", RoomName = "VEC-410" },
            new Room { Building = "VEC", RoomName = "VEC-411" },
            new Room { Building = "VEC", RoomName = "VEC-412" },
            new Room { Building = "VEC", RoomName = "VEC-413" }
            
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
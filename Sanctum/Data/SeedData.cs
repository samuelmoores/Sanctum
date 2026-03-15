using Sanctum.Models;

namespace Sanctum.Data;

public class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        var desiredRooms = new List<Room>
        {
            // ECS
            new Room { Building = "ECS", RoomName = "ECS-201" },
            new Room { Building = "ECS", RoomName = "ECS-202" },
            new Room { Building = "ECS", RoomName = "ECS-301" },
            // LIB
            new Room { Building = "LIB", RoomName = "LIB-101" },
            new Room { Building = "LIB", RoomName = "LIB-203" },
            new Room { Building = "LIB", RoomName = "LIB-305" },
            // USU
            new Room { Building = "USU", RoomName = "USU-110" },
            new Room { Building = "USU", RoomName = "USU-210" },
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
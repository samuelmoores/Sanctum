using Sanctum.Models;

namespace Sanctum.Data;

public class SeedData
{
    public static void Initialize(AppDbContext context)
    {
        // Only seed if table is empty
        if (context.Rooms.Any()) return;

        context.Rooms.AddRange(
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
            new Room { Building = "VEC", RoomName = "VEC-19" }
        );

        context.SaveChanges();
    }
}
// -----------------------------------------------------------------------
// RoomController.cs
// -----------------------------------------------------------------------
// This controller is responsible for handling all HTTP requests related
// to rooms in the Sanctum application. It sits between the database and
// the frontend, fetching room data and returning it in a format that
// JavaScript (site.js) can understand and use to populate the UI.
// -----------------------------------------------------------------------
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sanctum.Data;
namespace Sanctum.Controllers;

public class RoomController : Controller
{
    private readonly AppDbContext _context;
    
    public RoomController(AppDbContext _context)
    {
        this._context = _context;
    }

    [HttpGet]
    [Route("Room/GetRooms")]
    public async Task<IActionResult> GetRooms()
    {
        // Query the Rooms table asynchronously using Entity Framework Core.
        //
        // .GroupBy(r => r.Building)
        //   Groups all room records by their Building column value,
        //   e.g. all "ECS" rooms together, all "LIB" rooms together, etc.
        //
        // .ToDictionary(...)
        //   Converts the grouped results into a Dictionary<string, List<string>>
        //   where the key is the building name and the value is a list of
        //   room names belonging to that building.
        //
        //   g.Key         → the building name (e.g. "ECS")
        //   g.Select(...) → projects each room in the group to just its RoomName
        //   * had to change this because the new tests couldn't run against the EF
        //   Core InMemory provider, but the functionality is the same
        var rooms = (await _context.Rooms.ToListAsync())
            .GroupBy(r => r.Building)
            .ToDictionary(g => g.Key, g => g.Select(r => r.RoomName).ToList());

        // Json() serializes the rooms dictionary into a JSON response and
        // sets the Content-Type header to application/json. This is what
        // the fetch() call in site.js receives and parses into a JS object.
        return Json(rooms);
    }
}
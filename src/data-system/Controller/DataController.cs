using DataSystem.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.ObjectPool;

namespace DataSystem.Controller;

[Route("api/data")]
[ApiController]
public class DataController : ControllerBase {
    private readonly ApplicationContext _context;

    public DataController(ApplicationContext context)
    {
        _context = context;
    }

    //  GET: /api/Data
    [HttpGet]
    public async Task<ActionResult<SensorData>> GetData()
    {
        var result = _context.SensorData.FirstOrDefault();

        return result;
    }

}
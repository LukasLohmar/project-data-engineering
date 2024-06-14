using System.Net.NetworkInformation;
using DataSystem.Database;
using DataSystem.Database.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataSystem.Controller;

/// <summary>
/// API-Controller
/// </summary>
[Route("api/data")]
[ApiController]
public class DataController(ApplicationContext context) : ControllerBase {
    private readonly ApplicationContext _context = context;

    /// <summary>
    /// return entries ordered by descending timestamp
    /// example: "GET: /api/data/latest?deviceId=B827EBBF9D51&count=100&accessToken=130e1808-40a9-46d0-9b26-80d08fe3c55b"
    /// </summary>
    /// <param name="accessToken">the authorization token</param>
    /// <param name="deviceId">the deviceId of the selected device</param>
    /// <param name="count">maximum number of returned rows</param>
    /// <returns>dataset containing the matching rows when deviceId is specified, otherwise dataset containing rows ordered by descending timestamp. it contains a maximum number of rows specified by the count parameter</returns>
    [HttpGet("latest")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<SensorDataDto>>> GetLatestData(string accessToken, string? deviceId = null, int count = 100)
    {
        // check if token is set and valid
        if (string.IsNullOrEmpty(accessToken) || !Guid.TryParse(accessToken.AsSpan(), out var guid))
            return BadRequest();
        
        var authorizationEntry = await context.Authorization.Where(i => i.Token == guid && i.Locked == false && i.AuthorizedFlags.HasFlag(AuthorizeFlags.Read)).FirstOrDefaultAsync();

        // return if token is not allowed or locked
        if (authorizationEntry == null)
            return BadRequest();
        
        // get generic queryable when deviceId is not set
        var queryable = deviceId != null && PhysicalAddress.TryParse(deviceId, out var parsedDeviceId) ? context.SensorData.Where(i => i.DeviceId == parsedDeviceId) : context.SensorData.AsQueryable();

        var result = queryable.OrderByDescending(i => i.TimeStamp).Take(count).Select(i => new SensorDataDto(i));

        if (result.Any())
            return await result.ToListAsync();

        return NoContent();
    }
}
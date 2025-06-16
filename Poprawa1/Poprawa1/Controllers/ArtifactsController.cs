using Poprawa1.Services;
using Poprawa1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Poprawa1.Controllers;

[Route("api/[controller]")]
[ApiController]

public class ArtifactsController : ControllerBase
{
    private readonly IDbService _iDbService;

    public ArtifactsController(IDbService iDbService)
    {
        _iDbService = iDbService;
    }
    
    [HttpPost]
    public async Task<IActionResult> AddArtifactAsync([FromBody]InputDTO artifact)
    {

        try
        {
            await _iDbService.AddArtifactAndProjectAsync(artifact);

        }
        catch (ArgumentException argEx)
        {
            return NotFound(argEx.Message);
        }

        catch (InvalidOperationException ioEx)
        {
            return Conflict(ioEx.Message);
        }

        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }

        return StatusCode(201, artifact);
    }
}
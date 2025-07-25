using Poprawa1.Services;
using Poprawa1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Poprawa1.Controllers;

[Route("api/[controller]")]
[ApiController]

public class ProjectsController : ControllerBase
{
    private readonly IDbService _iDbService;

    public ProjectsController(IDbService iDbService)
    {
        _iDbService = iDbService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(int id)
    {
        try
        {
            var project = await _iDbService.GetProjectAsync(id);
            return Ok(project);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ApplicationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (SqlException ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
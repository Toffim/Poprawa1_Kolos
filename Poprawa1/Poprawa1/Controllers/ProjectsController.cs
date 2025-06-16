using Poprawa1.Services;
using Poprawa1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Poprawa1.Controllers;

[Route("api/[controller]")]
[ApiController]

public class ProjectsController : ControllerBase
{
    private readonly IProjectService _iProjectService;

    public ProjectsController(IProjectService iProjectService)
    {
        _iProjectService = iProjectService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(int id)
    {
        try
        {
            var project = await _iProjectService.GetProjectAsync(id);
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

    [HttpPost]
    public async Task<IActionResult> AddArtifactAsync([FromBody]InputDTO artifact)
    {

        try
        {
            await _iProjectService.AddArtifactAndProjectAsync(artifact);

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
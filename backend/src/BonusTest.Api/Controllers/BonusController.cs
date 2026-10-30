using BonusTest.Application.Bonuses;
using Microsoft.AspNetCore.Mvc;

namespace BonusTest.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class BonusController(BonusService bonusService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] BonusFilterRequest request,
        CancellationToken cancellationToken) =>
        Ok(await bonusService.GetPagedAsync(request, cancellationToken));

    [HttpGet("employee/{departmentCode}/{employeeNumber:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetEmployee(
        string departmentCode,
        int employeeNumber,
        CancellationToken cancellationToken) =>
        Ok(await bonusService.GetEmployeeAsync(departmentCode, employeeNumber, cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBonusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await bonusService.CreateAsync(request, UserCode(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{bonusId:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int bonusId, CancellationToken cancellationToken) =>
        Ok(await bonusService.CancelAsync(bonusId, UserCode(), cancellationToken));

    [HttpDelete("{bonusId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int bonusId, CancellationToken cancellationToken)
    {
        await bonusService.DeleteAsync(bonusId, UserCode(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{bonusId:int}/pay")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Pay(int bonusId, CancellationToken cancellationToken) =>
        Ok(await bonusService.PayAsync(bonusId, UserCode(), cancellationToken));

    private string? UserCode() => Request.Headers["UserCode"].FirstOrDefault();
}

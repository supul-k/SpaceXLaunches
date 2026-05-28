using Microsoft.AspNetCore.Mvc;
using SpaceXLaunches.Application.DTOs;
using SpaceXLaunches.Application.Services;
using System.Net;

namespace SpaceXLaunches.API.Controllers
{
    [Produces("application/json")]
    [Route("[controlller]")]
    public class LaunchesController : ControllerBase
    {
        private const string _basePath = "/api/launches";
        private readonly LaunchService _launchService;
        public LaunchesController(LaunchService launchService)
        {
            _launchService = launchService;
        }

        [HttpGet(_basePath)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                IEnumerable<LaunchDto> launches = await _launchService.GetAllLaunchesAsync();
                return Ok(launches);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { error = "Failed to reach SpaceX API.", detail = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        [HttpGet(_basePath + "/{id}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { error = "Id cannot be empty." });
            }

            try
            {
                LaunchDto? launch = await _launchService.GetLaunchByIdAsync(id);

                if (launch is null)
                {
                    return NotFound(new { error = $"No launch found with id '{id}'." });
                }

                return Ok(launch);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { error = "Failed to reach SpaceX API.", detail = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", detail = ex.Message });
            }
        }
    }
}

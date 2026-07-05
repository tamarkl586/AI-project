using IdentityService.BLL.Interfaces;
using IdentityService.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _service;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService service, ILogger<AuthController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDTO dto)
    {
        _logger.LogInformation("Attempting to register new user: {Email}", dto.Email);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Registration validation failed for {Email}.", dto.Email);
            return BadRequest(ModelState);
        }

        try
        {
            await _service.RegisterAsync(dto);
            _logger.LogInformation("User '{Email}' registered successfully.", dto.Email);
            return Ok(new { message = "הרישום בוצע בהצלחה! כעת ניתן להתחבר" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration conflict for {Email}: {Reason}", dto.Email, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during registration for {Email}", dto.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "שגיאה בתהליך הרישום" });
        }
    }

    [HttpGet("check-email")]
    public async Task<IActionResult> CheckEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(false);

        var exists = await _service.EmailExistsAsync(email);
        return Ok(exists);
    }

    [HttpGet("check-name")]
    public async Task<IActionResult> CheckName([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(false);

        var exists = await _service.NameExistsAsync(name);
        return Ok(exists);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDTO dto)
    {
        _logger.LogInformation("Login attempt for user: {Email}", dto.Email);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Login model validation failed for {Email}.", dto.Email);
            return BadRequest(ModelState);
        }

        try
        {
            var token = await _service.LoginAsync(dto);
            _logger.LogInformation("User '{Email}' logged in successfully.", dto.Email);
            return Ok(token);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized login attempt: {Email}. Reason: {Reason}", dto.Email, ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during login for {Email}", dto.Email);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "שגיאה בתהליך ההתחברות" });
        }
    }
}

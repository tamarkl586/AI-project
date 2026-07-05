using IdentityService.DTOs.Auth;

namespace IdentityService.BLL.Interfaces;

public interface IAuthService
{
    Task RegisterAsync(RegisterDTO dto);
    Task<string> LoginAsync(LoginDTO dto);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> NameExistsAsync(string name);
}

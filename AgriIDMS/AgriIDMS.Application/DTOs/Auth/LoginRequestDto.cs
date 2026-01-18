using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.DTOs.Auth
{
    public record LoginRequestDto(string UserNameOrEmail, string Password);
    public record LogoutRequestDto(string RefreshToken);

    public record AuthResponseDto(string AccessToken,string RefreshToken,string UserId,string UserName,IList<string> Roles);

    public record RefreshRequestDto(string RefreshToken);

}

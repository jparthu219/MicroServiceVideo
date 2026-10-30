using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Mango.Web.Models;
using Mango.Web.Service.IService;
using Mango.Web.Utility;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace Mango.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ITokenProvider _tokenProvider;

        public AuthController(IAuthService authService, ITokenProvider tokenProvider)
        {
            _authService = authService;
            _tokenProvider = tokenProvider;
        }

        [HttpGet]
        public IActionResult Login()
        {
            LoginRequestDto loginRequestDto = new();
            return View(loginRequestDto);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginRequestDto obj)
        {
            if (ModelState.IsValid)
            {
                ResponseDto? responseDto = await _authService.LoginAsync(obj);

                if (responseDto != null && responseDto.IsSuccess && responseDto.Result != null)
                {
                    LoginResponseDto? loginResponseDto =
                        JsonConvert.DeserializeObject<LoginResponseDto>(Convert.ToString(responseDto.Result)!);

                    if (loginResponseDto != null && !string.IsNullOrEmpty(loginResponseDto.Token))
                    {
                        await SignInUser(loginResponseDto);
                        _tokenProvider.SetToken(loginResponseDto.Token);
                        TempData["success"] = "Login successful";
                        return RedirectToAction("Index", "Home");
                    }
                }
                else
                {
                    TempData["error"] = responseDto?.Message ?? "Invalid login attempt.";
                }
            }

            return View(obj);
        }

        [HttpGet]
        public IActionResult Register()
        {
            var roleList = new List<SelectListItem>()
            {
                new SelectListItem{Text=SD.RoleAdmin, Value=SD.RoleAdmin},
                new SelectListItem{Text=SD.RoleCustomer, Value=SD.RoleCustomer},
            };

            ViewBag.RoleList = roleList;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegistrationRequestDto obj)
        {
            if (ModelState.IsValid)
            {
                ResponseDto? result = await _authService.RegisterAsync(obj);
                ResponseDto? assignRole;

                if (result != null && result.IsSuccess)
                {
                    if (string.IsNullOrEmpty(obj.Role))
                    {
                        obj.Role = SD.RoleCustomer;
                    }
                    assignRole = await _authService.AssignRoleAsync(obj);
                    if (assignRole != null && assignRole.IsSuccess)
                    {
                        TempData["success"] = "Registration successful";
                        return RedirectToAction(nameof(Login));
                    }
                    else
                    {
                        TempData["error"] = assignRole?.Message ?? "Role assignment failed.";
                    }
                }
                else
                {
                    TempData["error"] = result?.Message ?? "Registration failed.";
                }
            }

            var roleList = new List<SelectListItem>()
            {
                new SelectListItem{Text=SD.RoleAdmin, Value=SD.RoleAdmin},
                new SelectListItem{Text=SD.RoleCustomer, Value=SD.RoleCustomer},
            };

            ViewBag.RoleList = roleList;
            return View(obj);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            _tokenProvider.ClearToken();
            TempData["success"] = "Logged out successfully";
            return RedirectToAction("Index", "Home");
        }

        private async Task SignInUser(LoginResponseDto model)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(model.Token);

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);

            var emailClaim = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Email)?.Value
                ?? jwt.Claims.FirstOrDefault(u => u.Type == "email")?.Value
                ?? model.User?.Email ?? "";

            var subClaim = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub)?.Value
                ?? jwt.Claims.FirstOrDefault(u => u.Type == "sub")?.Value
                ?? model.User?.ID ?? "";

            var nameClaim = jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Name)?.Value
                ?? jwt.Claims.FirstOrDefault(u => u.Type == "name")?.Value
                ?? model.User?.Name ?? "";

            identity.AddClaim(new Claim(JwtRegisteredClaimNames.Email, emailClaim));
            identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, subClaim));
            identity.AddClaim(new Claim(JwtRegisteredClaimNames.Name, nameClaim));

            identity.AddClaim(new Claim(ClaimTypes.Name, !string.IsNullOrEmpty(nameClaim) ? nameClaim : emailClaim));
            identity.AddClaim(new Claim(ClaimTypes.Email, emailClaim));

            var roleClaims = jwt.Claims.Where(u => u.Type == "role" || u.Type == ClaimTypes.Role);
            foreach (var role in roleClaims)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role.Value));
            }

            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }
    }
}

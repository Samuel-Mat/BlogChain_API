using Microsoft.AspNetCore.Mvc;
using BlogChain_API.Services;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using BlogChain_API.Models.User;
using System.Web;

namespace BlogChain_API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UsersService _usersService;

        private readonly PasswordService _passwordService;

        private readonly IConfiguration _configuration;

        public UserController(UsersService usersService, PasswordService passwordService, IConfiguration configuration)
        {
             _usersService = usersService;
             _passwordService = passwordService;
            _configuration = configuration;
        }

        [HttpGet("Get-All")]
        public async Task<List<UserModel>> GetAll() =>
            await _usersService.GetAsync();

        [HttpGet("Get-Me"), Authorize]
        public async Task<IActionResult> GetMe()
        {
            UserModel user = await _usersService.GetById();
            return Ok(user);
        }
          

        [HttpGet("GetProfile"), Authorize]

        public async Task<ActionResult> GetProfile()
        {

            UserModel user = await _usersService.GetById();
            return File(user.ProfileImage, "image/png");
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(UserModelDto newUser)
        {
            try
            {
                UserModel user = new UserModel();
                user.Username = HttpUtility.HtmlEncode(newUser.Username);
                user.Password = _passwordService.HashPassword(newUser.Password);
                user.Email = HttpUtility.HtmlEncode(newUser.Email);

                if (await _usersService.GetWithUsername(HttpUtility.HtmlEncode(user.Username)) != null)
                {
                    return BadRequest("A User with this username already exists (╯°□°）╯︵ ┻━┻");
                }


                await _usersService.CreateAsync(user);

                return Ok(newUser);
            }catch (Exception ex)
            {
                return BadRequest($"Failed to register {ex.Message} (╯°□°）╯︵ ┻━┻");
            }
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(UserLoginDto userData)
        {
            if (userData == null)
            {
                return BadRequest("You forgot the username or password. (╯°□°）╯︵ ┻━┻");
            }

            UserModel? user = await _usersService.GetWithUsername(HttpUtility.HtmlEncode(userData.Username));

            if (user == null)
            {
                return BadRequest("Username or Password is wrong (╯°□°）╯︵ ┻━┻");
            }

            bool success = _passwordService.VerifyPassword(user, userData.Password);
            if (success)
            {
                string token = CreateToken(user);
                return Ok(token);
            }

            return BadRequest("Username or Password is wrong (╯°□°）╯︵ ┻━┻");
        }

        [HttpPatch("ChangeProfilePic"), Authorize]
        public async Task<IActionResult> ChangeProfilePic(IFormFile newImage)
        {

            var user = await _usersService.GetById();
            if (user == null)
            {
                return NotFound("(╯°□°）╯︵ ┻━┻");
            }
            using (MemoryStream ms = new MemoryStream())
            {

                newImage.CopyTo(ms);
                byte[] imageInBytes = ms.ToArray();
                user.ProfileImage = imageInBytes;
                await _usersService.UpdateAsync(user.Id, user);
               
            }
            return Ok("Uploaded Picture successfully");
        }

        [HttpPatch("ChangeDescription"), Authorize]
        public async Task<IActionResult> ChangeDescription(string description)
        {

            var user = await _usersService.GetById();
            if (user == null)
            {
                return NotFound("(╯°□°）╯︵ ┻━┻");
            }
            
            user.ProfileDescription = description;
            await _usersService.UpdateAsync(user.Id, user);
            return Ok("Description changed successfully");
        }


        [HttpDelete("DeleteAll")]
        public async Task<IActionResult> DeleteAll()
        {
            await _usersService.DeleteAll();
            return Ok();
        }

        private string CreateToken(UserModel user)
        {
            List<Claim> claims = new List<Claim>()
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username.ToString()),
                new Claim(ClaimTypes.Role, "User")
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("JWT:Key").Value!));

            var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: cred
                );
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
    }
}

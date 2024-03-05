using Microsoft.AspNetCore.Mvc;
using BlogChain_API.Services;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using BlogChain_API.Models.User;
using System.Web;
using static System.Runtime.InteropServices.JavaScript.JSType;
using MongoDB.Libmongocrypt;

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

        [HttpGet("GetProfile")]

        public async Task<ActionResult> GetProfile(string id)
        {

            UserModel user = await _usersService.GetSingle(id);

            if (user == null)
            {
                return NotFound("No user found (╯°□°）╯︵ ┻━┻");
            }
            GetSingleUserDto userDto = new GetSingleUserDto();
            userDto.Username = user.Username;
            userDto.ProfileImage = user.ProfileImage;
            return Ok(userDto);
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

                string base64String = "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";
                user.ProfileImage = Convert.FromBase64String(base64String);



                await _usersService.CreateAsync(user);

                return Ok(newUser);
            }
            catch (Exception ex)
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

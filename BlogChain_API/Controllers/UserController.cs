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

            if(user == null)
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

                string base64String = "iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAAXNSR0IArs4c6QAAAplJREFUSEu11UuollUUBuDnaKJCl4GEk7IwskiogZWJ0kQQNSeiKGkYRQMLHGQ18Ya3cuIFBC8QUWimjRwIagRORNSKKCjFEM1G4sCBiRlStt/D/uE7X/7+hwNnwcf/fftfe79rvetda/cZZusb5vP1AhiNBZiNF/Ak7uJ3/ITjZf0w/u4W6P0A5mI3nuiR5WW8V8H+59oNYDNWV+9T+BLn8Wtdm4zn8Cam1rW1yL4Bdi+ANdhU086mbfi3SxYj8BE2IHSuKr9bmr5tgFn4Bv+UlF/BD9X5UWzFa/X7CD7A9fr9Ek6XwEZiJk50QJoAY/EbHqv0fFKd4vM9prSyyNrLjbVkuxGXKn39hW8CvIH9RTFnML1By5xy0FH8UeqwuPD8AL7C40jG31aQ0HUWL5ZAl+BgG+BrLMJbRY5fNCILFaEntfiwrm/H+5WmvHfsHXyKA0jAAzIIPU+XGkyrWXQ2pQeO1QwWYhQSTKhsZhD/GaUGJ6viorIBAH+WSB8skT5UIr3ZiCo0pqmeb9UgAkhxmxYxXMMNPNINIMW+3do4rmzYUVQ1rwjgTq1JU0Ud9+y9VahLsA+3AS6WIj9VnyhhKDYJF+rzbLciv4u9QzkdK7CzqmxpN5meQ0ZBLAVdVhrn1SqA/sLhl6KSZJyC7qu0pVYZJ8+UDF7HoTZA+MvgGl/lmC5djwk9srlSOj9NFlWlOa8W0In4qw2Q7+UFYE/rwKjic2TopXtjUU+a8W1EOU1LL3zWWbjXsNtVx298cmhmS7d5P6bOnfROLEpb2UTrNq7X1QkZ3/Cc4fUdfqybM5cyhwKeTGKZwh+36ex14YSuXjUY0oXTCSQzfn6JPrdb88pMYX+uDTfkK7OHeAb3d69Lf3Cn3MfrP1DxfRmUVLOqAAAAAElFTkSuQmCC";
                user.ProfileImage = Convert.FromBase64String(base64String);



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

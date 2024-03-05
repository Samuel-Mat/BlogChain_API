using BCrypt.Net;
using BlogChain_API.Models.User;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text;

namespace BlogChain_API.Services
{
    public class PasswordService
    {


        private readonly UsersService _usersService;

        public PasswordService(UsersService usersService)
        {
            {
                _usersService = usersService;
            }
        }
        public string HashPassword(string password)
        {
			string salt = BCrypt.Net.BCrypt.GenerateSalt();

			
			string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password, salt);
			
            return hashedPassword;
        }


        public bool VerifyPassword(UserModel user, string password) 
        {
            try
            {
                Console.WriteLine(user.Password);
                Console.WriteLine(password);
                if (user == null)
                {
                    return false;
                }

                if(BCrypt.Net.BCrypt.Verify(password, user.Password) == true)
                {
                    Console.WriteLine("Success");
                    return true;
                }

                Console.WriteLine("No Success");
                return false;


            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        
    }
}

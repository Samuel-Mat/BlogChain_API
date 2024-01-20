using BCrypt.Net;
using BlogChain_API.Models.User;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text;

namespace BlogChain_API.Services
{
    public class PasswordService
    {
        private string _salt = "$ä¨paüptüosgklksecapybaraajteutcapybaracapybaracapybaracapybaracapybaracapybaracapybaracapybaracapybaracapybara";

        private readonly UsersService _usersService;

        public PasswordService(UsersService usersService)
        {
            {
                _usersService = usersService;
            }
        }
        public string HashPassword(string password)
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(_salt+password);
            return hashedPassword;
        }


        public bool VerifyPassword(UserModel user, string password) 
        {
            try
            {
                Console.WriteLine(_salt);
                if (user == null)
                {
                    return false;
                }

                if(BCrypt.Net.BCrypt.Verify(_salt+password, user.Password))
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

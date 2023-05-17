using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Practica.Data;
using Practica.Data.Entities;
using Practica.DTO;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace Practica.Controllers
{
    [Route("" +
        "[controller]")]
    [ApiController]
    public class AccountController : ControllerBase

    {
        private readonly SocialMediaDb _db;
        private readonly IConfiguration _config;
        

        public AccountController(SocialMediaDb db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        
        [HttpPost("register")]
        public async Task<IActionResult> PostAsync([FromBody] RegisterDTO payload)
        {
            var check = _db.Users
                .Where(u => u.Name == payload.Name)
                .SingleOrDefault();
            if(check is not null)
            {
                Ok(StatusCode(404));
            }
            string base64hashedPasswordBytes;
            using (var sha256 = SHA256.Create())
            {
                var passwordBytes = Encoding.UTF8.GetBytes(payload.Password);
                var hashedPasswordBytes = sha256.ComputeHash(passwordBytes);
                base64hashedPasswordBytes = Convert.ToBase64String(hashedPasswordBytes);
            }
            User user = new User
            {
                Name = payload.Name,
                Email = payload.Email,
                PhoneNumber = payload.Number,
                BirthDate = DateTime.Now,
                HashedPassword = base64hashedPasswordBytes,
                ProfileImg = "https://univovidius-my.sharepoint.com/:i:/g/personal/iulian_amelian_365_univ-ovidius_ro/EWAqvpPbyHBCh_ULIp67JpcBwlZgEr4bPXm31xXECAYd-Q?e=a1KeCp"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var jwt = GenerateJSONWebToken(user);

            return new JsonResult(new { jwt });
        }
        [HttpPut("updateData")]
        [Authorize]
        public ActionResult UpdateData(string name, string email, DateTime birthDate, int number)
        {
            var userToEdit = _db.Users
                .Where(u => u.Name == name)
                .SingleOrDefault();

            if (userToEdit == null)
            {
                return NotFound();
            }

            userToEdit.Name = name;
            userToEdit.Email = email;
            userToEdit.BirthDate = birthDate;
            userToEdit.PhoneNumber = number;
            _db.SaveChanges();

            return NoContent();
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public ActionResult Login([FromBody] LoginDTO payload)
        {
            string base64hashedPasswordBytes;
            using (var sha256 = SHA256.Create())
            {
                var passwordBytes = Encoding.UTF8.GetBytes(payload.Password);
                var hashedPasswordBytes = sha256.ComputeHash(passwordBytes);
                base64hashedPasswordBytes = Convert.ToBase64String(hashedPasswordBytes);
            }

            var existingUser = _db.Users
                .Where(u => u.Name == payload.UserName
                         && u.HashedPassword == base64hashedPasswordBytes)
                .SingleOrDefault();
            if (existingUser is null)
            {
                return NotFound();
            }
            else
            {
                var jwt = GenerateJSONWebToken(existingUser);


                return new JsonResult(new { jwt , user = payload.UserName}) ;
            }
            return Unauthorized();
        }
        [HttpGet("GetUsers")]
        [Authorize]
        public ActionResult GetUsers()
        {
            var result = _db.Users.OrderBy(x => x.Name).ToList();
            if(result.IsNullOrEmpty())
            {
                return Unauthorized();
            }
            else
            {
                return Ok(result);
            }
        }
        [HttpGet("GetUserByName")]
        public ActionResult GetUserByName(string name)
        {
            var existingUser = _db.Users
                .Where(u => u.Name == name)
                .SingleOrDefault();
            if (existingUser is null)
            {
                return Unauthorized();
            }
            else
            {

                return Ok(existingUser);
            }   
        }
        [HttpDelete("DeleteUser")]
        [Authorize]
        public IActionResult Delete(string username, string password)
        {
            var entityToDelete = _db.Users.FirstOrDefault(x => x.Name == username && x.HashedPassword == createHashedPassword(password));

            if (entityToDelete == null)
            {
                return NotFound();
            }

            _db.Users.Remove(entityToDelete);
            _db.SaveChanges();

            return NoContent();
        }

        private string GenerateJSONWebToken(User userInfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Email,userInfo.Email),
                new Claim("profile_picture_url","..."),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                _config["Jwt:Issuer"],
                claims,
                expires: DateTime.Now.AddMinutes(120),
                signingCredentials: credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private string createHashedPassword(string password)
        {
            string base64hashedPasswordBytes;
            using (var sha256 = SHA256.Create())
            {
                var passwordBytes = Encoding.UTF8.GetBytes(password);
                var hashedPasswordBytes = sha256.ComputeHash(passwordBytes);
                base64hashedPasswordBytes = Convert.ToBase64String(hashedPasswordBytes);
            }
            return base64hashedPasswordBytes;
        }
    }
}

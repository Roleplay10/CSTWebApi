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
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
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
        [AllowAnonymous]
        public async Task<IActionResult> PostAsync([FromBody] RegisterDTO payload)
        {
            var check = _db.Users
                .Where(u => u.Name == payload.Name)
                .SingleOrDefault();
            if (check is not null)
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
            int[] dates = payload.birthDate.Split(".").Select(int.Parse).ToArray();
            if (dates.Length != 3)
            {
                return NoContent();
            }
            User user = new User
            {
                Name = payload.Name,
                Email = payload.Email,
                PhoneNumber = payload.Number,
                BirthDate = new DateTime(dates[2], dates[1], dates[0]),
                HashedPassword = base64hashedPasswordBytes
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var jwt = GenerateJSONWebToken(user);

            return new JsonResult(new { jwt });
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
            if (existingUser is not null)
            {
                var jwt = GenerateJSONWebToken(existingUser);


                return new JsonResult(new { jwt, user = payload.UserName });
            }
            else
            {
                return Unauthorized();
            }
        }

        [HttpPut("modifyPassword")]
        [Authorize]
        public ActionResult UpdatePassword(string name,string old_password,string new_password)
        {
            var userToEdit = _db.Users
                .Where(u => u.Name == name)
                .SingleOrDefault();

            if (userToEdit == null)
            {
                return NotFound();
            }

            if(createHashedPassword(old_password) != userToEdit.HashedPassword)
            {
                return new ContentResult
                {
                    Content = "Invalid credentials",
                    ContentType = "text/plain",
                    StatusCode = 401
                };
            }

            userToEdit.HashedPassword = createHashedPassword(new_password);

            _db.SaveChanges();

            return Ok();
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
        [HttpPost("AddPost")]
        [Authorize]
        public ActionResult createPost(int userId, string title, string description)
        {
            var user = _db.Users.SingleOrDefault(u => u.Id == userId);

            

            if (user is null)
            {
                return NotFound("User not found.");
            }

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(description))
            {
                return BadRequest("Title and description must not be empty.");
            }

            Post post = new Post
            {
                Title = title,
                Description = description,
                UserId = userId,
                User = user
            };


            user.Posts.Add(post);
            _db.SaveChanges();

            return Ok(post);

        }

        [HttpGet("GetPotsByUserName")]
        public ActionResult getPostByName(string name)
        {
            var existingUser = _db.Users
                .Where(u => u.Name == name)
                .Include(post => post.Posts)
                .SingleOrDefault();
            if(existingUser is null)
            {
                return NotFound();
            }

            var posts = existingUser.Posts.Select(p => new PostDTO
            {
                Id = p.Id,
                UserId = p.UserId,
                Title = p.Title,
                Description = p.Description
            }).ToList();

            return Ok(posts);
        }
        [HttpGet("GetAllPosts")]
        public ActionResult getPosts()
        {
            var posts = _db.Posts.Select(p => new PostDTO
            {
                Id = p.Id,
                UserId = p.UserId,
                Title = p.Title,
                Description = p.Description
            }).ToList();
            if (posts is null)
            {
                return NotFound();
            }

            return Ok(posts);
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

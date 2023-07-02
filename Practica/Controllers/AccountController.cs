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
        public ActionResult UpdateData(string name, string email, string birthDate, int number)
        {
            var userToEdit = _db.Users
                .Where(u => u.Name == name)
                .SingleOrDefault();

            if (userToEdit == null)
            {
                return NotFound();
            }

            int[] date = birthDate.Split(".").Select(int.Parse).ToArray();
            userToEdit.Name = name;
            userToEdit.Email = email;
            userToEdit.BirthDate = new DateTime(date[2], date[1], date[0]);
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
        [Authorize]
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
        [HttpPost("CreatePost")]
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
        [Authorize]
        public ActionResult getPostByName(string name)
        {
            var existingUser = _db.Users
                .Where(u => u.Name == name)
                .Include(p => p.Posts)
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
        [HttpGet("GetPotsByPostId")]
        [Authorize]
        public ActionResult getPostById(int id)
        {
            var post = _db.Posts
                .Where(u => u.Id == id)
                .SingleOrDefault();
            if (post is null)
            {
                return NotFound();
            }

            var posts = new PostDTO
            {
                Id = post.Id,
                UserId = post.UserId,
                Title = post.Title,
                Description = post.Description
            };

            return Ok(posts);
        }
        [HttpGet("GetAllPosts")]
        [Authorize]
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
        [HttpPost("CreateReaction")]
        [Authorize]
        public ActionResult createReaction(int postId,int userId,string type)
        {
            var post = _db.Posts
                .Where(u => u.Id == postId)
                .SingleOrDefault();
            if(post is null)
            {
                return NotFound();
            }
            string[] allowed_reactions = { "like", "love" };
            if (!allowed_reactions.Contains(type))
            {
                return NotFound("This reaction doesn't exists");
            }
            var reaction = new Reaction
            {
                ReactionType = type,
                UserId = userId,
                PostId = post.Id
            };
            post.Reactions.Add(reaction);
            _db.SaveChanges();
            return Ok("Reaction created succesfully");
        }
        [HttpPost("CreateComment")]
        [Authorize]
        public ActionResult createComment(int postId, int userId, string content)
        {
            var post = _db.Posts
                .Where(u => u.Id == postId)
                .SingleOrDefault();
            if (post is null)
            {
                return NotFound();
            }
            var comment = new Comment
            {
                content = content,
                UserId = userId,
                PostId = post.Id
            };
            post.Comments.Add(comment);
            _db.SaveChanges();
            return Ok("Comment created succesfully");
        }
        [HttpGet("GetAllReactionFromPostId")]
        [Authorize]
        public ActionResult getAllReactPostId(int postId)
        {
            var post = _db.Posts
                .Where(u => u.Id == postId)
                .Include(u=> u.Reactions)
                .SingleOrDefault();
            
            if (post is null)
            {
                return NotFound();
            }
            var reacts = post.Reactions.Select(p => new ReactionDTO
            {
                Id = p.Id,
                ReactionType = p.ReactionType,
                UserId= p.UserId,
                PostId = p.PostId
            }).ToList();
            return Ok(reacts);
        }
        [HttpGet("GetAllCommentsFromPostId")]
        [Authorize]
        public ActionResult getAllCommentsPostId(int postId)
        {
            var post = _db.Posts
                .Where(u => u.Id == postId)
                .Include(u => u.Comments)
                .SingleOrDefault();

            if (post is null)
            {
                return NotFound();
            }
            var comments = post.Comments.Select(p => new CommentDTO
            {
                Id = p.Id,
                content = p.content,
                UserId = p.UserId,
                PostId = p.PostId
            }).ToList();
            return Ok(comments);
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

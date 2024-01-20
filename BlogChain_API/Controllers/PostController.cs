using BlogChain_API.Models.Post;
using BlogChain_API.Models.User;
using BlogChain_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;

namespace BlogChain_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostController : ControllerBase
    {
        private readonly UsersService _usersService;

        private readonly PostService _postService;

        private readonly IHttpContextAccessor _contextAccessor;


        public PostController(UsersService usersService, PostService postService, IHttpContextAccessor contextAccessor)
        {
            _usersService = usersService;
            _postService = postService;
            _contextAccessor = contextAccessor;
        }

        [HttpGet("GetAll")]
        public async Task<List<PostModel>> GetAll()
        {
            return await _postService.GetAsync();
        }

        [HttpGet("GetMyPosts"), Authorize]
        public async Task<List<PostModel>> GetMyPosts()
        {
            return await _postService.GetOwn();
        }

        [HttpPost("CreateTextPost"), Authorize]
        public async Task<IActionResult> CreateTextPost(CreatePostModelTextDto postData)
        {
            UserModel author = await _usersService.GetById();
            PostModel newPost = new PostModel();
            newPost.Text = postData.Text;
            newPost.Published = DateTime.UtcNow;
            newPost.AuthorId = author.Id.ToString();

            author.Posts.Add(newPost);
            await _usersService.UpdateAsync(author.Id.ToString(), author);
            await _postService.CreateAsync(newPost);

            return Ok("Post successfully created");
        }

        [HttpPost("CreateImagePost"), Authorize]
        public async Task<IActionResult> CreateImagePost(IFormFile postData)
        {
            UserModel author = await _usersService.GetById();
            PostModel newPost = new PostModel();

            using (MemoryStream ms = new MemoryStream())
            {

                postData.CopyTo(ms);
                byte[] imageInBytes = ms.ToArray();
                newPost.Image = imageInBytes;
            }

            newPost.Published = DateTime.UtcNow;
            newPost.AuthorId = author.Id.ToString();

            author.Posts.Add(newPost);
            await _usersService.UpdateAsync(author.Id.ToString(), author);
            await _postService.CreateAsync(newPost);

            return Ok("Post successfully created");
        }

        [HttpPatch("AddComment"), Authorize]
        public async Task<IActionResult> AddComment(string postId, string text)
        {
            UserModel author = await _usersService.GetById();
            PostModel post = await _postService.GetSingle(postId);

            CommentModel comment = new CommentModel();
            comment.Text = text;
            comment.AuthorId = author.Id.ToString();
            comment.Published = DateTime.UtcNow;
            comment.Id = ObjectId.GenerateNewId().ToString();

            await _postService.AddComment(post, comment);
            return Ok("Comment added successfully");
        }

        [HttpDelete("DeletePost"), Authorize]
        public async Task<IActionResult> DeletePost(string id)
        {
            try
            {
                PostModel post = await _postService.GetSingle(id);
                if (post.AuthorId == _contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString())
                {
                    await _postService.RemoveAsync(id);
                    return Ok($"Post with id {id} successfully deleted");
                }
                else
                {
                    return Unauthorized("You are not authorized to delete this post!");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("DeleteAll")]
        public async Task<IActionResult> DeleteAll()
        {
            await _postService.DeleteAll();
            return Ok();
        }
    }
}

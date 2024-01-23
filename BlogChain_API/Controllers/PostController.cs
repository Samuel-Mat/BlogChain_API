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

        [HttpPatch("AddLike"), Authorize]
        public async Task<IActionResult> AddLike(string postId)
        {
            PostModel post = await _postService.GetSingle(postId);
            UserModel liker = await _usersService.GetById();

            bool alreadyLiked = post.LikedBy.Contains(liker.Id.ToString());

            if (alreadyLiked)
            {
                return BadRequest("You Already liked this post");
            }

            post.LikedBy.Add(liker.Id.ToString());
            await _postService.UpdateAsync(postId, post);
            return Ok("post liked");
        }

        [HttpPatch("RemoveLike"), Authorize]
        public async Task<IActionResult> RemoveLike(string postId)
        {
            PostModel? post = await _postService.GetSingle(postId);

            if (post == null)
            {
                return NotFound($"The post with id {postId} doesn't exist");
            }
            UserModel liker = await _usersService.GetById();

            bool alreadyLiked = post.LikedBy.Contains(liker.Id.ToString());

            if (alreadyLiked)
            {
                post.LikedBy.Remove(liker.Id.ToString());
                await _postService.UpdateAsync(postId, post);
                return Ok("Removed Like");
            }
           
            return BadRequest("You didn't like the post");
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

        [HttpPatch("RemoveComment"), Authorize]
        public async Task<IActionResult> RemoveComment(string commentId, string postId)
        {
            try
            {
                UserModel user = await _usersService.GetById();
                PostModel? post = await _postService.GetSingle(postId);

                if (post == null)
                {
                    return NotFound("No post with this Id has been found");
                }

                CommentModel? comment = post.Comments.FirstOrDefault(x => x.Id.ToString() == commentId);


                if (comment == null)
                {
                    return NotFound("No comment with this Id has been found");
                }

                if (comment.AuthorId == user.Id || post.AuthorId == user.Id)
                {
                    post.Comments.Remove(comment);
                    await _postService.UpdateAsync(postId, post);
                    return Ok("Removed Comment");
                }

                return Unauthorized("You are not authorized to remove this comment");
            }catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
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

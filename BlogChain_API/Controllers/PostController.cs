using BlogChain_API.Models.Post;
using BlogChain_API.Models.User;
using BlogChain_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System;
using System.Security.Claims;
using System.Web;

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

            if (author == null)
            {
                return BadRequest("Something with your AuthorID is wrong (╯°□°）╯︵ ┻━┻");
            }

            try
            {


                PostModel newPost = new PostModel();

                newPost.Text = HttpUtility.HtmlEncode(postData.Text);
                newPost.Published = DateTime.UtcNow.ToString();
                newPost.AuthorId = author.Id.ToString();
                newPost.Id = BsonObjectId.GenerateNewId().ToString();
                newPost.AuthorName = author.Username;

                await _postService.CreateAsync(newPost);

                author.Posts.Add(newPost);
                await _usersService.UpdateAsync(author.Id.ToString(), author);


                return Ok("Post successfully created");

            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to create the post: {ex.Message} (╯°□°）╯︵ ┻━┻");
            }
        }

        [HttpPost("CreateImagePost"), Authorize]
        public async Task<IActionResult> CreateImagePost(IFormFile postData)
        {
            UserModel author = await _usersService.GetById();

            if (author == null)
            {
                return BadRequest("Something with your AuthorID is wrong (╯°□°）╯︵ ┻━┻");
            }

            if (postData == null)
            {
                return BadRequest("No Image selected (╯°□°）╯︵ ┻━┻");
            }

            PostModel newPost = new PostModel();

            try
            {

                using (MemoryStream ms = new MemoryStream())
                {

                    postData.CopyTo(ms);
                    byte[] imageInBytes = ms.ToArray();
                    newPost.Image = imageInBytes;
                }

                newPost.Published = DateTime.UtcNow.ToString();
                newPost.AuthorId = author.Id.ToString();
                newPost.AuthorName = author.Username;

                author.Posts.Add(newPost);
                await _usersService.UpdateAsync(author.Id.ToString(), author);
                await _postService.CreateAsync(newPost);

                return Ok("Post successfully created");

            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to create the post: {ex.Message} (╯°□°）╯︵ ┻━┻");
            }
        }

        [HttpPatch("AddLike"), Authorize]
        public async Task<IActionResult> AddLike(string postId)
        {
            PostModel post = await _postService.GetSingle(postId);
            UserModel liker = await _usersService.GetById();

            bool alreadyLiked = post.LikedBy.Contains(liker.Id.ToString());

            if (alreadyLiked)
            {
                return BadRequest("You Already liked this post (╯°□°）╯︵ ┻━┻");
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
                return NotFound($"The post with id {postId} doesn't exist (╯°□°）╯︵ ┻━┻");
            }
            UserModel liker = await _usersService.GetById();

            bool alreadyLiked = post.LikedBy.Contains(liker.Id.ToString());

            if (alreadyLiked)
            {
                post.LikedBy.Remove(liker.Id.ToString());
                await _postService.UpdateAsync(postId, post);
                return Ok("Removed Like");
            }

            return BadRequest("You didn't like the post (╯°□°）╯︵ ┻━┻");
        }

        [HttpPatch("AddComment"), Authorize]
        public async Task<IActionResult> AddComment(string postId, string text)
        {
            UserModel author = await _usersService.GetById();
            PostModel post = await _postService.GetSingle(postId);

            CommentModel comment = new CommentModel();
            comment.Text = HttpUtility.HtmlEncode(text);
            comment.AuthorId = author.Id.ToString();
            comment.Published = DateTime.UtcNow.ToString();
            comment.Id = ObjectId.GenerateNewId().ToString();
            comment.AuthorName = author.Username;

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
                    return NotFound("No post with this Id has been found (╯°□°）╯︵ ┻━┻");
                }

                CommentModel? comment = post.Comments.FirstOrDefault(x => x.Id.ToString() == commentId);


                if (comment == null)
                {
                    return NotFound("No comment with this Id has been found (╯°□°）╯︵ ┻━┻");
                }

                if (comment.AuthorId == user.Id || post.AuthorId == user.Id)
                {
                    post.Comments.Remove(comment);
                    await _postService.UpdateAsync(postId, post);
                    return Ok("Removed Comment");
                }

                return Unauthorized("You are not authorized to remove this comment (╯°□°）╯︵ ┻━┻");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("SavePost"), Authorize]

        public async Task<IActionResult> SavePost(string postId)
        {
            UserModel user = await _usersService.GetById();

            PostModel post = await _postService.GetSingle(postId);

            if (post == null)
            {
                return NotFound("No post with this Id has been found (╯°□°）╯︵ ┻━┻");
            }

            PostModel alreadySaved = user.SavedPosts.FirstOrDefault(x => x.Id == postId);


            if (alreadySaved != null)
            {
                return BadRequest("Already saved this post (╯°□°）╯︵ ┻━┻");
            }

            user.SavedPosts.Add(post);
            await _usersService.UpdateAsync(user.Id.ToString(), user);
            return Ok("Saved post");
        }

        [HttpPatch("UnsavePost"), Authorize]

        public async Task<IActionResult> UnsavePost(string postId)
        {
            UserModel user = await _usersService.GetById();

            PostModel post = await _postService.GetSingle(postId);

            if (post == null)
            {
                return NotFound("No post with this Id has been found (╯°□°）╯︵ ┻━┻");
            }

            PostModel alreadySaved = user.SavedPosts.FirstOrDefault(x => x.Id == postId);

            if (alreadySaved != null)
            {
                PostModel deletepost = user.SavedPosts.FirstOrDefault(x => x.Id == post.Id);
                user.SavedPosts.Remove(deletepost);
                await _usersService.UpdateAsync(user.Id.ToString(), user);
                return Ok("Unsaved post");
            }

            return BadRequest("You didn't save this post (╯°□°）╯︵ ┻━┻");
        }

        [HttpDelete("DeletePost"), Authorize]
        public async Task<IActionResult> DeletePost(string id)
        {
            try
            {
                UserModel user = await _usersService.GetById();
                PostModel post = await _postService.GetSingle(id);
                if (post.AuthorId == user.Id.ToString())
                {
                    RemoveFromAllSaves(post);

                    PostModel deletePost = user.Posts.Find(x => x.Id == post.Id);
                    user.Posts.Remove(deletePost);
                    await _usersService.UpdateAsync(user.Id, user);

                    await _postService.RemoveAsync(id);


                    return Ok($"Post with id {id} successfully deleted");
                }
                else
                {
                    return Unauthorized("You are not authorized to delete this post! (╯°□°）╯︵ ┻━┻");
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

        private async void RemoveFromAllSaves(PostModel post)
        {
            List<UserModel> users = await _usersService.GetAsync();


            foreach (UserModel user in users)
            {
                Console.WriteLine(user);
                PostModel deletePost = user.SavedPosts.FirstOrDefault(x => x.Id == post.Id);
                if (deletePost != null)
                {
                    user.SavedPosts.Remove(deletePost);
                    await _usersService.UpdateAsync(user.Id.ToString(), user);
                }


            }
        }
    }
}

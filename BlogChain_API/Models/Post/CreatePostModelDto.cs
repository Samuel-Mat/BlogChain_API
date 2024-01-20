namespace BlogChain_API.Models.Post
{
    public class CreatePostModelTextDto
    {
        public string Text { get; set; }
    }

    public class CreatePostModelImageDto
    {
        public IFormFile Image { get; set; }
    }
}

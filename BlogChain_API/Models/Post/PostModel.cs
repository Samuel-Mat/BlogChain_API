using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using BlogChain_API.Models.User;

namespace BlogChain_API.Models.Post
{
    public class PostModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Text { get; set; }

        public byte[] Image { get; set; }

        public HashSet<string> LikedBy { get; set; }

        public DateTime Published { get; set; }

        public List<CommentModel> Comments { get; set; } = new List<CommentModel>();   

        public string AuthorId { get; set; }
    }
}

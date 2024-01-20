using BlogChain_API.Models.Post;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace BlogChain_API.Models.User
{
    public class UserModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Username { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public byte[] ProfileImage { get; set; }

        public string ProfileDescription { get; set; }

        public List<PostModel> Posts { get; set; } = new List<PostModel>();
    }
}

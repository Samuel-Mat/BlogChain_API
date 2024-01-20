using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace BlogChain_API.Models.Post
{
    public class CommentModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Text { get; set; }

        public DateTime Published { get; set; }

        public string AuthorId { get; set; }
    }
}

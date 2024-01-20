using BlogChain_API.Models;
using BlogChain_API.Models.Post;
using BlogChain_API.Models.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using System.Security.Claims;

namespace BlogChain_API.Services
{
    public class PostService
    {
        private readonly IMongoCollection<PostModel> _postCollection;

        private readonly IHttpContextAccessor _contextAccessor;

        public PostService(IOptions<BlogChainDBSettings> blogChainDBSettings, IHttpContextAccessor contextAccessor)
        {
            var mongoClient = new MongoClient(
                blogChainDBSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                blogChainDBSettings.Value.DatabaseName);

            _postCollection = mongoDatabase.GetCollection<PostModel>(
                blogChainDBSettings.Value.PostsCollectionName);
            _contextAccessor = contextAccessor;
        }

        public async Task<List<PostModel>> GetAsync() =>
        await _postCollection.Find(_ => true).ToListAsync();

        
        public async Task<List<PostModel>> GetOwn()
        {
            string id = _contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString();
            return await _postCollection.Find(x => x.AuthorId.ToString() == id).ToListAsync();
        }

        public async Task<PostModel> GetSingle(string id)
        {
            return await _postCollection.Find(x => x.Id.ToString() == id).FirstOrDefaultAsync();
        }

        public async Task AddComment(PostModel post, CommentModel comment)
        {
            post.Comments.Add(comment);
            await _postCollection.ReplaceOneAsync(x => x.Id == post.Id, post);
        }

        public async Task CreateAsync(PostModel newPost) =>
            await _postCollection.InsertOneAsync(newPost);

        public async Task UpdateAsync(string id, PostModel updatedPost) =>
            await _postCollection.ReplaceOneAsync(x => x.Id == id, updatedPost);

        public async Task RemoveAsync(string id) =>

            await _postCollection.DeleteOneAsync(x => x.Id == id);

        public async Task DeleteAll()
        {
            await _postCollection.DeleteManyAsync(x => x.Id != null);
        }
    }
}


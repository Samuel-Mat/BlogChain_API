using Microsoft.Extensions.Options;
using MongoDB.Driver;
using BlogChain_API.Models;
using System.Security.Claims;
using BlogChain_API.Models.User;

namespace BlogChain_API.Services
{
    public class UsersService
    {
        private readonly IMongoCollection<UserModel> _usersCollection;

        private readonly IHttpContextAccessor _contextAccessor;

        public UsersService(IOptions<BlogChainDBSettings> blogChainDBSettings, IHttpContextAccessor contextAccessor)
        {
            var mongoClient = new MongoClient(
                blogChainDBSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                blogChainDBSettings.Value.DatabaseName);

            _usersCollection = mongoDatabase.GetCollection<UserModel>(
                blogChainDBSettings.Value.UsersCollectionName);

            _contextAccessor = contextAccessor;
        }

        public async Task<List<UserModel>> GetAsync() =>
        await _usersCollection.Find(_ => true).ToListAsync();

        public async Task<UserModel?> GetById()
        {
            string id = _contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value.ToString();
            return await _usersCollection.Find(x => x.Id.ToString() == id).FirstOrDefaultAsync();
        }
    

        public async Task<UserModel?> GetWithUsername(string username) =>
         await _usersCollection.Find(x => x.Username == username).FirstOrDefaultAsync();

        public async Task CreateAsync(UserModel newUser) =>
            await _usersCollection.InsertOneAsync(newUser);

        public async Task UpdateAsync(string id, UserModel updatedUser) =>
            await _usersCollection.ReplaceOneAsync(x => x.Id == id, updatedUser);

        public async Task RemoveAsync(string id) =>
            await _usersCollection.DeleteOneAsync(x => x.Id == id);

        public async Task DeleteAll()
        {
            await _usersCollection.DeleteManyAsync(x => x.Id != null);
        }
    }
}


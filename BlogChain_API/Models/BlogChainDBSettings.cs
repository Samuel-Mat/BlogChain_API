namespace BlogChain_API.Models
{
    public class BlogChainDBSettings
    {
        public string ConnectionString { get; set; } = null!;

        public string DatabaseName { get; set; } = null!;

        public string UsersCollectionName { get; set; } = null!;

        public string PostsCollectionName { get; set; } = null!;
    }
}

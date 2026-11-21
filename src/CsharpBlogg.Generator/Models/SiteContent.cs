namespace CsharpBlogg.Generator.Models
{
    internal class SiteContent
    {
        public SiteConfig SiteConfig { get; set; } = new SiteConfig();
        public List<Post> Posts { get; set; } = [];
        public List<Page> Pages { get; set; } = [];
        public List<ContentItem> PostsAndPages => new List<ContentItem>(Posts).Concat(Pages).ToList();
        public List<ContentItem> PublishedPosts { get; set; } = [];
        public List<ContentItem> PublishedPages { get; set; } = [];
        public List<ContentItem> PublishedSlugs { get; set; } = [];
        public List<string> AllTags { get; set; } = [];

        public SiteContent(SiteConfig siteConfig, List<Post> posts, List<Page> pages)
        {
            SiteConfig = siteConfig;
            Posts = posts;
            Pages = pages;
            PublishedPosts = new List<ContentItem>(Posts).Where(c => c.Published != null && c.Published <= DateTime.Now && !string.IsNullOrEmpty(c.Slug) && !c.Draft).ToList();
            PublishedPages = new List<ContentItem>(Pages).Where(c => c.Published != null && c.Published <= DateTime.Now && !string.IsNullOrEmpty(c.Slug) && !c.Draft).ToList();
            PublishedSlugs = new List<ContentItem>(PublishedPosts).Concat(PublishedPages).ToList();
            AllTags = [.. posts.SelectMany(p => p.Tags).Distinct()];
        }
    }
}

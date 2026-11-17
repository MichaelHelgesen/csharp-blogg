namespace CsharpBlogg.Generator.Models
{
    internal class SiteContent
    {
        public SiteConfig SiteConfig { get; set; } = new SiteConfig();
        public List<Post> Posts { get; set; } = [];
        public List<Page> Pages { get; set; } = [];
        public List<ContentItem> PostsAndPages => new List<ContentItem>(Posts).Concat(Pages).ToList();
        public List<string> AllTags { get; set; } = [];

        public SiteContent(SiteConfig siteConfig, List<Post> posts, List<Page> pages)
        {
            SiteConfig = siteConfig;
            Posts = posts;
            Pages = pages;
            AllTags = [.. posts.SelectMany(p => p.Tags).Distinct()];
        }
    }
}

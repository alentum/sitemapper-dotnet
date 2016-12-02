namespace RobotsTxt
{
    public interface IRobotsParser
    {
        bool IsPathAllowed(string userAgent, string path);
        long CrawlDelay(string userAgent);
    }
}
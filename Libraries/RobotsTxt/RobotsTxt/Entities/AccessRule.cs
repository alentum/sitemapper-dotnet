using System;

namespace RobotsTxt
{
    internal class AccessRule : Rule
    {
        public string Path { get; private set; }
        public bool Allowed { get; private set; }

        public AccessRule(string userAgent, Line line, int order)
            : base(userAgent, order)
        {
            Path = line.Value;
            // Fixed from patch
            if (!String.IsNullOrEmpty(Path) && !Path.StartsWith("/") && !Path.StartsWith("*"))
            {
                Path = "/" + Path;
            }

            Allowed = line.Field.ToLowerInvariant().Equals("allow");
        }
    }
}
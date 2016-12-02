namespace RobotsTxt
{
    internal abstract class Rule
    {
        public string For { get; private set; }
        public int Order { get; private set; }

        public Rule(string userAgent, int order)
        {
            For = userAgent;
            Order = order;
        }
    }
}
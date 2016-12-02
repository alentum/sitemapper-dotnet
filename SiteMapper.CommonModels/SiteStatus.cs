using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiteMapper.CommonModels
{
    public enum SiteStatus
    {
        Added = 0,
        Processed = 1,
        ProcessedWithProblems = 2,
        Processing = 3,
        ConnectionProblem = 4,
        RobotsTxtProblem = 5
    }
}

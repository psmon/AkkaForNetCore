using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Dispatch;

namespace AkkaNetCore.Models.Message
{
    public class Issue
    {
        public bool IsSecurityFlaw;
        public bool IsBug;
        public string Message;

        public Issue(string message, bool isBug)
        {
            Message = message;
            IsBug = isBug;
            IsSecurityFlaw = !isBug;
        }
    }

    public class IssueTrackerMailbox : UnboundedPriorityMailbox
    {
        public IssueTrackerMailbox(Settings setting, Hocon.Config config) : base(setting, config)
        {
        }

        protected override int PriorityGenerator(object message)
        {
            var issue = message as Issue;

            if (issue != null)
            {
                if (issue.IsSecurityFlaw)
                    return 0;

                if (issue.IsBug)
                    return 1;
            }

            return 2;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading;

namespace IRCBot.Bot.Database
{
    public class ParsingException : Exception
    {
        public ParsingException(string message)
            : base(message)
        {
        }

        public ParsingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}

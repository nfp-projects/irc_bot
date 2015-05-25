using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading;

namespace IRCBot.Bot.Database
{
    class SerializerException : Exception
    {
        object _data;

        public SerializerException(string message, object data)
            : this(message, data, null)
        {
        }

        public SerializerException(string message, object data, Exception innerException)
            : base(message, innerException)
        {
            _data = data;
        }

        public new object Data
        {
            get { return _data; }
        }
    }
}

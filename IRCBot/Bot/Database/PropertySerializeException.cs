using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Xml.Serialization;
using System.Text;
using System.Threading;

namespace IRCBot.Bot.Database
{
    class PropertySerializeException : Exception
    {
        private readonly PropertyInfo _property;

        public PropertySerializeException(string message, PropertyInfo property)
            : this(message, property, null)
        {
        }

        public PropertySerializeException(string message, PropertyInfo property, Exception innerException)
            : base(message, innerException)
        {
            _property = property;
        }

        public PropertyInfo Property
        {
            get { return _property; }
        }
    }
}

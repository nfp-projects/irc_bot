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
    public class PropertySerializer
    {
        private readonly string _unserialised;
        private readonly object _data;
        private PropertyInfo _propertyInfo;

        public PropertySerializer()
        {
            SerializedObjects = new List<object>();
        }

        public PropertySerializer(object data, string serialisedProperty)
            : this()
        {
            _data = data;
            _unserialised = serialisedProperty;
        }

        public PropertySerializer(object data, PropertyInfo propInfo)
            : this()
        {
            _data = data;
            _propertyInfo = propInfo;
        }

        public string Serialize()
        {
            if (_propertyInfo == null)
                throw new NullReferenceException("An attempt was made serialize a null referenced property.");

            Serializer ser = new Serializer(SerializedObjects);
            switch (_propertyInfo.GetIndexParameters().Length)
            {
                case 0:
                    return String.Format("{0}={1}", _propertyInfo.Name, ser.Serialize(_propertyInfo.GetValue(_data, null)));
                case 1:
                    if (_data is IList)
                    {
                        string output = "";
                        for (int i = 0; i < (_data as IList).Count; i++)
                            if (i > 0)
                                output += "," + ser.Serialize((_data as IList)[i]);
                            else
                                output += ser.Serialize((_data as IList)[i]);
                        return String.Format("{0}=[{1}]", _propertyInfo.Name, output);
                    }
                    throw new PropertySerializeException("An attempt was made to serialize a property that required one index parameter but the object was not of an IList interface.", _propertyInfo);
                default:
                    throw new PropertySerializeException("An attempt was made to serialize a property that required more than one index parameter. This is currently unsupported.", _propertyInfo);
            }
        }

        public static bool CanSerializeProperty(PropertyInfo info)
        {
            if (info.CanWrite)
                if (info.GetCustomAttributes(typeof(XmlIgnoreAttribute), true).Length == 0)
                    return true;
                else
                    return false;
            if (typeof(IList).IsAssignableFrom(info.PropertyType))
                if (info.GetCustomAttributes(typeof(XmlIgnoreAttribute), true).Length == 0)
                    return true;
            return false;
        }

        public List<object> SerializedObjects { get; set; }
    }
}

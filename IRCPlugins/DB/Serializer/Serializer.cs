using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.Serialization;

namespace IRCPlugin.DB.Serializer
{
    /// <summary>
    /// Public class to serialize or deserialize objects.
    /// </summary>
    public class Serializer
    {
        private object _data;
        private Type _dataType;
        private List<object> _serializedObjects;
        private static Dictionary<Type, PropertyInfo[]> _cachedProperties;

        /// <summary>
        /// Initialise a new instance of Serializer.
        /// </summary>
        public Serializer()
        {
            _serializedObjects = new List<object>();
            if (_cachedProperties == null)
                _cachedProperties = new Dictionary<Type, PropertyInfo[]>();
        }

        /// <summary>
        /// Initialise a new instance of Serializer with specific values.
        /// </summary>
        /// <param name="serializedObjects">A list of already serialized values. This contains reference to a
        /// collection of already serliased objects so if this serializer encounters instances to serialize
        /// already serialized values it will link to it.</param>
        public Serializer(List<object> serializedObjects)
            : this()
        {
            _serializedObjects = serializedObjects;
        }

        /// <summary>
        /// Seralise an object.
        /// </summary>
        /// <param name="data">The object itself that is being serialized.</param>
        /// <returns>A string representing a serialized value of the object.</returns>
        public string Serialize(object data)
        {
            /*
             * This serializer works a little different that normal serializer. All objects are formatted as 
             * following and recursively:
             *	Basic value types:
             *		FullNameOfTheObject:TheValue
             *		
             *			If we are trying to parse an integer of value 4, the output would be:
             *				Int32:4
             *			If we were trying to parse a double value of 4.3645 the output would be:
             *				Double:4.3645
             *			Before the semicolon is the name of the type without the "System." in front
             *			and after the semicolon comes the value parsed using the ToString() method.
             *	
             *	Arrays:
             *		FullNameOfTheObjectPlusNamespace:[...]
             *		
             *			The dots are comma seperated values that contain the serialized value of the objects
             *			inside the array. If we have an array of integers containing the values of 4, 6 and 3
             *			the output would be like so:
             *				System.Int32[]:[Int32:4,Int32:6,Int32:3]
             *			The value of each contains the name of the type and it is so because then we
             *			can have an aray of objects which contain an integer of 5, a double value of 6.43 and
             *			a custom object. With this the result would be like so:
             *				System.Object[]:[Int32:5,Double:6.43,NameOfNamespace.OurObject:{...}]
             *			For more information on how objects are parsed look below.
             *
             *	Custom objects and such:
             *		FullNameOfTheObjectPlusNamespace:{...}
             *		
             *			Inside the brackets comes all properties that are supported in the following serialized format:
             *				NameOfProperty=xxx;
             *			The xxx represent the value run through the serializer recursively, so if we have a property
             *			called 'NumberOfUnits' and has an integer value of 2, the output would be following:
             *				NumberOfUnits=Int32:2;
             *
             */

            //Basic check to see if the data really is data.
            if (data != null)
            {
                //Create a local copy of the data.
                _data = data;

                //Check to make sure we are not serialising an already serialized objects
                if (_serializedObjects.Contains(data))
                {
                    //Object contained a reference to itself but was not an INetworkData object.
                    //In order to fully support circular reference, the object must be INetworkData.
                    //This can also happen if 2 different objects contain a reference to the same object.
                    throw new SerializerException("A circular reference was detected on an object that was not of type INetworkData. Make sure all circular reference objects are of type INetworkData. This also applies if 2 different objects contain a reference to the same object then that object must also be an INetworkData object.", data);
                }

                //Create a local copy of the type of the object.
                _dataType = _data.GetType();

                //The serialized value of the object
                string output = "";

                //If the object being serialized is a basic System type we don't need to write
                //the full name of it. Instead for example 'System.Int32' we get 'Int32', short
                //and simple.
                if (_dataType.FullName == "System." + _dataType.Name)
                    output += _dataType.Name + ":";
                else
                    if (_dataType.UnderlyingSystemType != null && _dataType.UnderlyingSystemType.Name.Contains("ReadOnly"))
                        output += _dataType.DeclaringType.FullName + ":";
                    else
                        output += _dataType.FullName + ":";

                //If the object is a basic value type, all we have to do is run ToString method
                //and be done with it.
                if (_data is string)
                    return String.Format("{0}\"{1}\"", output, (_data as string).Replace("\"", "\\\""));
                else if (_data is ValueType)
                    return output + (_data).ToString();

                //Check to see if the object is an array or a collection of some sort.
                if (_data is Array)
                {
                    //We are parsing an array, prepare a new serializer and parse all of it's values.
                    output += "[";
                    Serializer ser = new Serializer();
                    for (int i = 0; i < (_data as IList).Count; i++)
                        //The contents of an array is a comma seperated value of the contents.
                        if (i > 0)
                            output += "," + ser.Serialize((_data as IList)[i]);
                        else
                            output += ser.Serialize((_data as IList)[i]);
                    return output + "]";
                }
                else
                {
                    //We are parsing a normal object. Parse all supported properties inside the object.
                    output += "{";

                    //We create a cache of all valid properties of a given type due to the nature that
                    //checking whether a property is supported or not is a slow process.
                    if (!_cachedProperties.ContainsKey(_dataType))
                    {
                        CachePropertiesFromType(_dataType);
                    }

                    //Save our object into the collection. If any property contains a reference to itself
                    //then we will know about it by checking if it exists in the collection.
                    _serializedObjects.Add(data);

                    //Run through each property of the object and parse it's value
                    foreach (var propInfo in _cachedProperties[_dataType])
                    {
                        //PropertSerializer takes care of this for us.
                        PropertySerializer serProp = new PropertySerializer(_data, propInfo)
                        {
                            SerializedObjects = _serializedObjects
                        };
                        output += serProp.Serialize() + ";";
                    }
                    return output + "}";
                }
            }
            else
                return "null";
        }

        /// <summary>
        /// Cache all supported properties for a specific type.
        /// </summary>
        /// <param name="type">The type to cache all supported properties for.</param>
        private static void CachePropertiesFromType(Type type)
        {
            //We have yet to create a local cache of all supported properties of 
            //the type of the object being serialized. Prepare a new list and
            //prepare to fill it with all properties we can support.
            List<PropertyInfo> listProp = new List<PropertyInfo>();
            foreach (var propInfo in type.GetProperties())
                //Check to see if the property can be serialized or not
                if (PropertySerializer.CanSerializeProperty(propInfo))
                    //The property can be serialized, add it to the list.
                    listProp.Add(propInfo);
            //Add the list of supported properties into our local cache.
            _cachedProperties.Add(type, listProp.ToArray());
        }

        /// <summary>
        /// Deserialize a string value into it's given object that it represents.
        /// </summary>
        /// <param name="data">A serialized object that should be deserialized.</param>
        /// <returns>The original object the serialized string represented.</returns>
        /// <exception cref="System.NullReferenceException" />
        /// <exception cref="NetworkLibrary.Exceptions.ParsingException" />
        /// <exception cref="NetworkLibrary.Exceptions.NetworkDataCollectionException" />
        public object Deserialize(string data)
        {
            return Deserialize(null, data);
        }

        /// <summary>
        /// Deserialize a string value into it's given object that it represents.
        /// </summary>
        /// <param name="t">The type of the data that is being deserialized.</param>
        /// <param name="data">A serialized object that should be deserialized.</param>
        /// <returns>The original object the serialized string represented.</returns>
        /// <exception cref="System.NotSupportedException" />
        /// <exception cref="System.NullReferenceException" />
        /// <exception cref="NetworkLibrary.Exceptions.ParsingException" />
        /// <exception cref="NetworkLibrary.Exceptions.NetworkDataCollectionException" />
        protected object Deserialize(Type dataType, string data)
        {
            object output = null;
            //Check to see if the data is longer than zero letters. this is done to prevent unnecessary exceptions
            //that could be thrown by the system.
            if (data.Length > 0)
            {
                //Check to see if we are parsing a serialized value of null
                if (data == "null")
                    return null;

                //Retrieve the local instance of NetworkHandler. We use this to get types registered
                //into the library as well as sending requests and such.
                //INetworkDataHandler networkHandler = ObjectFactory.GetInstance<INetworkDataHandler>();

                //Create a reference to the type being deserialized here.
                Type t;
                string[] split;

                if (dataType == null)
                {
                    //Split the name of the type and the value which is being separated by a semicolon.
                    split = data.Split(new char[] { ':' }, 2);

                    //Check to see if we were able to split the value.
                    if (split.Length == 2)
                    {
                        //If the type is a basic value type, the namespace 'System' is omitted to save space.
                        //Here we check to see if such omitation has been done and if so, add the System into
                        //the front of the name before we search for the type using name search.
                        if (split[0].IndexOf('.') == -1)
                            t = Type.GetType("System." + split[0]);

                        else if (split[0].EndsWith("[]"))
                        {
                            //Because this is an array we could be dealing with an array of special objects.
                            //These kinds of arrays objects can't be created directly but need to be passed
                            //through the Array.CreateInstance.
                            if (Database.RegisteredTypes.ContainsKey(split[0].Remove(split[0].Length - 2, 2)))
                                //Create an array of special objects.
                                t = Array.CreateInstance(Database.RegisteredTypes[split[0].Remove(split[0].Length - 2, 2)], 0).GetType();
                            else
                                //If it's an array of basic value types then we can create those directly.
                                t = Type.GetType(split[0]);
                        }
                        //Check if this type has already been registered.
                        else if (Database.RegisteredTypes.ContainsKey(split[0]))
                            t = Database.RegisteredTypes[split[0]];

                        //Cross our fingers and hope we can create the type directly.
                        else
                            t = Type.GetType(split[0]);
                    }
                    else
                        throw new ParsingException("A serialized string was of an invalid format. Expecting a value of 'type:content'.");
                }
                else
                {
                    t = dataType;
                    split = new string[] { "", data };
                }

                //If we still don't have the type of the object there is nothing to be done.
                //Most likely cause for this is if the programmer forgot to pre-register the type
                //of the object.
                if (t == null)
                    throw new NullReferenceException(string.Format("Unable to create an object of name '{0}'. Did you forget to register the type?.", split[0]));

                //Check to see if the type is array
                if (t.IsArray)
                    //Because it's an array we can't dynamically add to it, we therefore create
                    //a list which we can add and remove at will and later, copy it into an array.
                    output = new List<object>();
                //If you try to run Activator and create an instance of string it will fail.
                else if (t != typeof(string))
                    //Create a default instance of the object being deserialized, we fill it with
                    //data later on.
                    output = Activator.CreateInstance(t);
                else
                    //The type is string, since we only need a basic value in the correct format,
                    //we make our output "become" string like so
                    output = "";

                int skipIdentifier = 0, length = split[1].Length - 1;
                bool insideParenthis = false;
                string identifiers = "{[]}", buffer = "", property = "";

                //Check to see if we are working with basic value type
                if (output is ValueType || output is string)
                    //Since this is just a basic value type, all we have to do is parse the value
                    //into it's correct format.
                    return ParseValueToType(t, split[1]);
                //Check to see if the Activator was successful in creating an instance for us.
                else if (output == null)
                    throw new NullReferenceException(string.Format("While creating an instance of '{0}', the activator returned null value.", t.FullName));
                else if (output is IList)
                    length++;

                //Add our new object to the collection
                _serializedObjects.Add(output);

                //Run through the value, feed the buffer and parse if necessary.
                for (int i = 1; i < length; i++)
                {
                    switch (split[1][i])
                    {
                        //The following is checking whether we have reached an end mark that resembles
                        //an end when parsing objects and a next step is necessary.
                        case ';':
                        case '=':
                            //Here we check if the end mark is really an end mark and not a character inside the value
                            if (skipIdentifier == 0 && !insideParenthis)
                                //Check to see if we have the name of the property or not.
                                if (string.IsNullOrEmpty(property))
                                {
                                    //The buffer contains the name of the property, retrieve it and flush
                                    //the buffer so it can grab the value of the property.
                                    property = buffer;
                                    buffer = "";
                                }
                                else
                                {
                                    //We have the name of the property we are trying to fill and the value
                                    //is inside the buffer. Prepare to deserialize the value and feed it into
                                    //the property of the object.

                                    //Grab a local info copy of the property.
                                    PropertyInfo info = t.GetProperty(property);

                                    //Deserialize the value
                                    object value;
                                    if (buffer[0] == '[')
                                        value = Deserialize(t, buffer);
                                    else
                                        value = Deserialize(buffer);

                                    //Check if the property is of an array or collection that can't be overridden.
                                    if (info.PropertyType.IsArray || !info.CanWrite)
                                        //Make sure the collection or array inside the object is not null.
                                        if (info.GetValue(output, null) == null)
                                        {
                                            //Because the collection or array inside is null and we can't override
                                            //it we stop what we are doing and continue with the rest of the deserializing.
                                            property = buffer = "";
                                            break;
                                        }

                                    //If we have direct write access, we can just pass the value directly
                                    //into the object and be done with it.
                                    if (info.CanWrite)
                                    {
                                        //Check to see if the property has index parameter
                                        if (info.GetIndexParameters().Length > 0)
                                        {
                                            //The property requires index parameter. This usually means we are working
                                            //with the contents of a collection. If so, then the value should also be a
                                            //collection of the contents.
                                            if (output is IList && !t.IsArray && value is IList)
                                                //Add each item into the collection
                                                for (int addRange = 0; addRange < (value as IList).Count; addRange++)
                                                    (output as IList).Add((value as IList)[addRange]);
                                            else
                                                //It required an index parameter but it wasn't a collection
                                                throw new NotSupportedException("Encountered a property that required index parameter but either the object or the value was not of an IList type.");
                                        }
                                        else
                                        {
                                            //No index property, then we just write the value directly
                                            info.SetValue(output, value, null);
                                        }
                                    }
                                    else if (value is IList)
                                        //We don't have direct write access but it is a collection
                                        //so instead of overwriting the array with new array, we fill
                                        //the array with new information from the value.
                                        if (info.PropertyType.IsArray)
                                        {
                                            //Grab the array from the collection.
                                            IList objectArray = info.GetValue(output, null) as IList;

                                            //Go over both arrays and make sure we don't go overboard.
                                            for (int arrayIndex = 0; arrayIndex < objectArray.Count && arrayIndex < (value as IList).Count; arrayIndex++)
                                                //Assign the value to the array in the object.
                                                objectArray[arrayIndex] = (value as IList)[arrayIndex];
                                        }
                                        else
                                        {
                                            //Grab the collection from the object.
                                            IList collection = info.GetValue(output, null) as IList;

                                            //Check to see if we have all the properties for this object cached.
                                            if (!_cachedProperties.ContainsKey(value.GetType()))
                                                //We don't have the properties for this object cached. We need to cache it
                                                //before we continue.
                                                CachePropertiesFromType(value.GetType());

                                            //Go over all the properties and merge them with the one inside the object.
                                            //By doing this, properties such as NetworkId will be passed along.
                                            foreach (var propInfo in _cachedProperties[value.GetType()])
                                                //Check if it's a basic property which we can write to.
                                                if (propInfo.GetIndexParameters().Length == 0 && propInfo.CanWrite)
                                                    //Write the property from our network packet collection
                                                    //into the collection inside the object.
                                                    propInfo.SetValue(collection, propInfo.GetValue(value, null), null);

                                            //Add the contents into the collection in the object.
                                            for (int item = 0; item < (value as IList).Count; item++)
                                                collection.Add((value as IList)[item]);
                                        }
                                    else
                                        throw new ParsingException("Encountered a property that is unsupported.");
                                    property = buffer = "";
                                }
                            else
                                //Since this is not an end mark, make sure we add it to the buffer.
                                buffer += split[1][i];
                            break;
                        //The following is checking whether we have reached an end mark that resembles
                        //an end when parsing arrays and such.
                        case ']':
                        case ',':
                            //Here we check if the end mark is really an end mark and not a character inside the value
                            if (skipIdentifier == 0 && output is IList && !insideParenthis)
                            {
                                if (buffer != "")
                                    //Since this is an array, we add the current value inside the buffer
                                    //into the output array and continue on.
                                    (output as IList).Add(Deserialize(buffer));
                                buffer = "";
                            }
                            else
                            {
                                if (!insideParenthis && split[1][i] == ']')
                                    skipIdentifier--;

                                //Since this is not an end mark, make sure we add it to the buffer.
                                buffer += split[1][i];
                            }
                            break;
                        //This checks to see if we have reached a string value, if so, all characters
                        //that resemble an end mark are automatically ignored. This is done by marking the
                        //insideParenthis boolean.
                        case '"':
                            //Check to see if the parenthesis has been escaped or not.
                            if (split[1][i - 1] != '\\')
                                //The parenthesis has not been escaped, this means it's a start or an end
                                //of a string value, mark this by changing the marked.
                                insideParenthis = !insideParenthis;

                            //Add the current character to the buffer.
                            buffer += split[1][i];
                            break;
                        default:
                            //Check to see if we have to skip some identifiers because we are adding
                            //items into the buffer that contain recursive objects.
                            if (identifiers.IndexOf(split[1][i]) > -1 && !insideParenthis)
                                //Check to see if we have to skip an extra identifier/endmark of if we
                                //are finishing skipping an identifier/endmark
                                if (identifiers.IndexOf(split[1][i]) < identifiers.Length / 2)
                                    skipIdentifier += 1;
                                else
                                    skipIdentifier -= 1;
                            buffer += split[1][i];
                            break;
                    }
                }
                //Since the output is array, here we copy the content of the collection into an array
                if (output is IList && t.IsArray)
                {
                    object temp = Activator.CreateInstance(t, (output as IList).Count);
                    Array.Copy((output as List<object>).ToArray(), temp as Array, (output as IList).Count);
                    return temp;
                }
            }

            return output;
        }

        /// <summary>
        /// Parse a string value from it's string type into a specific type. Uses Parse if such a method is available
        /// automatically parses enums from it's string values to it's original value.
        /// </summary>
        /// <param name="type">The type the string should be parsed into.</param>
        /// <param name="value">The string that represent the data that is being parsed.</param>
        /// <returns>The original value parsed into it's correct format.</returns>
        /// <exception cref="NetworkLibrary.Exceptions.ParsingException" />
        public static object ParseValueToType(Type type, string value)
        {
            //Create a basic holder for our parsed value
            object output = null;

            //If the value should be parsed into string, there is nothing for us to do.
            if (type == typeof(string))
                //We remove the first and last letter due to the fact that Serializer parses
                //all strings with a " in front of it and back to distinguish it from other types.
                return value.Substring(1, value.Length - 2);

            //Check whether the type has a method called Parse and accepts string as a parameter.
            //All basic values, like integers and such all support this.
            else if (type.GetMethod("Parse", new Type[] { typeof(string) }) != null)
            {
                try
                {
                    //Parse string using the internal Parse method the type supports.
                    output = type.GetMethod("Parse", new Type[] { typeof(string) }).Invoke(null, new object[] { value });
                }
                catch (Exception e)
                {
                    throw new ParsingException(string.Format("Error while parsing '{0}' to the following type: {1}. Error message received: {2}", value, type.FullName, e.Message));
                }
            }
            //If the type is a basic enum, we have to do a parse on it using naming convenience.
            //Fortunately C# supports this.
            else if (type.BaseType == typeof(Enum))
                return Enum.Parse(type, value);

            //Return the value parsed into correct format.
            return output;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using System.Data.SQLite;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IRCPlugin.DB
{
    public class Database : IDisposable
    {
        private SQLiteConnection _connection;
        private static Dictionary<string, Type> _registeredTypes = new Dictionary<string, Type>();

        public Database(string filename)
        {
            _connection = new SQLiteConnection(String.Format("Data Source={0};Version=3;New=True;", filename));
            _connection.Open();
        }

        public Database(string filename, bool addOptions)
            : this(filename)
        {
            Table<Option>().EnsureExists();
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
        }

        public SQLiteConnection Connection
        {
            get { return _connection; }
        }

        public T GetOption<T>(string name)
        {
            Serializer.Serializer serializer = new Serializer.Serializer();
            var option = Table<Option>().Get(name);
            if (option == null || option.Value == null)
                return default(T);
            return (T)serializer.Deserialize(option.Value);
        }

        public dynamic GetOption(string name)
        {
            Serializer.Serializer serializer = new Serializer.Serializer();
            var option = Table<Option>().Get(name);
            if (option == null || option.Value == null)
                return null;
            return serializer.Deserialize(option.Value);
        }

        public void SetOption<T>(string name, T value)
        {
            Serializer.Serializer serializer = new Serializer.Serializer();
            Table<Option>().Set(name, new Option { Value = serializer.Serialize(value) });
        }

        public void SetOption(string name, dynamic value)
        {
            Serializer.Serializer serializer = new Serializer.Serializer();
            Table<Option>().Set(name, new Option { Value = serializer.Serialize(value) });
        }

        public Table<T> Table<T>()
        {
            return new Table<T>(this);
        }

        /// <summary>
        /// A dictionary containing all registered types.
        /// </summary>
        public static Dictionary<string, Type> RegisteredTypes
        {
            get { return _registeredTypes; }
        }

        /// <summary>
        /// Register a type of an object to the database. This allows the transmit of that type of packets over the network.
        /// </summary>
        /// <param name="type">The type of an object to add.</param>
        public static void RegisterType(Type type)
        {
            if (!_registeredTypes.ContainsKey(type.FullName))
                _registeredTypes.Add(type.FullName, type);
        }

        /// <summary>
        /// Register all types of all available objects into the library from current assembly.
        /// </summary>
        public static void RegisterTypeFromAssembly()
        {
            RegisterTypeFromAssembly(Assembly.GetCallingAssembly());
        }

        /// <summary>
        /// Register all types of all available objects into the library from an assembly. Scans the assembly and automatically registers
        /// all types found into the Library.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        public static void RegisterTypeFromAssembly(Assembly assembly)
        {
            foreach (Type t in assembly.GetTypes())
                RegisterType(t);
        }
    }
}

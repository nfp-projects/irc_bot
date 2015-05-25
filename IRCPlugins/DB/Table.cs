using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dapper;
using System.Data.SQLite;
using System.Text;
using System.Threading.Tasks;

namespace IRCPlugin.DB
{
    public struct TableCache
    {
        public PropertyInfo[] Properties;
        public PropertyInfo Primary;
        public string Columns;
        public string ValuesWithId;
        public string Values;
    }

    public class Table<T>
    {
        protected static readonly Dictionary<Type, TableCache> _mapCache = new Dictionary<Type, TableCache>();
        protected static readonly Dictionary<string, string> _typeMap = new Dictionary<string, string>()
        {
            {"System.Byte", "INTEGER" },
            {"System.Boolean", "INTEGER" },
            {"System.Int16", "INTEGER" },
            {"System.Int32", "INTEGER" },
            {"System.Decimal", "REAL" },
            {"System.Int64", "INTEGER" },
            {"System.Double", "REAL" },
            {"System.Single", "REAL" },
            {"System.DateTime", "TEXT" },
            {"System.String", "TEXT " },
            {"System.Char", "TEXT" }
        };

        private readonly Database _database;
        private readonly TableCache _cache;

        public Table(Database database)
        {
            _database = database;

            if (!_mapCache.ContainsKey(typeof(T)))
            {
                TableCache cache = new TableCache();
                //Get primary key property
                cache.Properties = typeof(T).GetProperties();
                var primary = cache.Properties.Where(p => p.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Length > 0).FirstOrDefault();
                if (primary == null)
                {
                    primary = cache.Properties.Where(p => p.Name.Equals("Id", StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                }
                cache.Primary = primary;
                var others = cache.Properties.Where(p => p != cache.Primary);
                cache.Columns = string.Join(", ", cache.Properties.Select(k => string.Format("[{0}]", k.Name)));
                cache.Values = string.Join(", ", others.Select(k => string.Format("@{0}", k.Name)));
                if (cache.Primary != null)
                {
                    cache.ValuesWithId = string.Format("COALESCE((SELECT {0} FROM {1} WHERE {0} = @{0}), @{0}),", cache.Primary.Name, typeof(T).Name) + cache.Values;
                    cache.Values = string.Format("(SELECT {0} FROM {1} WHERE {0} = @{0}),", cache.Primary.Name, typeof(T).Name) + cache.Values;
                }

                _mapCache.Add(typeof(T), cache);
            }
            _cache = _mapCache[typeof(T)];
        }

        public Table<T> EnsureExists()
        {
            string tableName = typeof(T).Name;

            var others = _cache.Properties.Where(p => p != _cache.Primary );
            var columns = GetDelimitedSafeColumnList(", ", others);
            if (_cache.Primary != null)
                columns = string.Join(", ", GetColumn(_cache.Primary) + " NOT NULL", columns, string.Format("PRIMARY KEY({0})", _cache.Primary.Name));
            _database.Connection.Execute(string.Format("CREATE TABLE IF NOT EXISTS {0} ({1})", tableName, columns));
            return this;
        }

        public T Get(dynamic id)
        {
            if (_cache.Primary == null)
                throw new NullReferenceException("Cannot query by id of a non-primary key object.");

            var result = _database.Connection.Query<T>(string.Format("select * from {0} where {1} = @Id", typeof(T).Name, _cache.Primary.Name), new
            {
                Id = id
            }).SingleOrDefault();
            return result;
        }

        public IEnumerable<T> Select()
        {
            return Select(new { });
        }

        public IEnumerable<T> Select(dynamic query)
        {
            var properties = (IEnumerable<PropertyInfo>)query.GetType().GetProperties();
            string where = string.Join(", ", properties.Select(k => string.Format("{0} = @{0}", k.Name)));
            if (!string.IsNullOrEmpty(where))
                where = " WHERE " + where;
            var result = _database.Connection.Query<T>(string.Format("select * from {0}" + where, typeof(T).Name, _cache.Primary.Name), (object)query);
            return result;
        }

        public void Set(T item)
        {
            if (_cache.Primary != null && _typeMap.ContainsKey(_cache.Primary.PropertyType.FullName) && _typeMap[_cache.Primary.PropertyType.FullName] != "INTEGER")
            {
                Set(null, item);
                return;
            }
            _database.Connection.Execute(string.Format("INSERT OR REPLACE INTO [{0}]({1}) VALUES ({2})", typeof(T).Name, _cache.Columns, _cache.Values), item);
        }

        public void Set(dynamic id, T item)
        {
            if (id != null)
                _cache.Primary.SetValue(item, id);
            _database.Connection.Execute(string.Format("INSERT OR REPLACE INTO [{0}]({1}) VALUES ({2})", typeof(T).Name, _cache.Columns, _cache.ValuesWithId), item);
        }

        public void Save(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                Set(item);
            }
        }

        protected string GetDelimitedSafeColumnList(string delim, IEnumerable<PropertyInfo> properties)
        {
            return string.Join(delim, properties.Select(k => GetColumn(k)));
        }

        protected string GetColumn(PropertyInfo info)
        {
            string type = "";
            if (!_typeMap.TryGetValue(info.PropertyType.FullName, out type))
                type = "BLOB";
            return string.Format("{0} {1}", info.Name, type);
        }
    }
}

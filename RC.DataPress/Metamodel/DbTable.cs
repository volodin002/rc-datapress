using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RC.DataPress.Metamodel
{
    public class DbTable
    {
        public readonly string Name;
        public readonly string Schema;

        public DbTable(string name, string schema)
        {
            Name = name;
            Schema = schema;
        }
    }

    /*
    public readonly struct DbTable : IEquatable<DbTable>
    {
        public readonly string Name;
        public readonly string Schema;
        //public readonly int Index;

        //private static int _index;

        public DbTable(string name, string schema)
        {
            Name = name;
            Schema = schema;

            //Index = Interlocked.Increment(ref _index);
        }

        public DbTable(string name)
        {
            Name = name;
            Schema = null;

            //Index = Interlocked.Increment(ref _index);
        }

        public DbTable((string Name, string Schema) name)
        {
            Name = name.Name;
            Schema = name.Schema;

            //Interlocked.Increment(ref _index);
        }

        public void Deconstruct(out string name, out string schema)
        {
            name = Name;
            schema = Schema;
        }

        public override bool Equals(object obj)
        {
            if (obj is DbTable x)
            {
                return this.Equals(x);
            }
            return false;
        }

        public bool Equals(DbTable table) => (Name == table.Name) && (Schema == table.Schema);
        

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                return 23 * (17 * 23 + Name.GetHashCode()) + 
                    (Schema != null ? Schema.GetHashCode() : 0);
            }
        }

        public static bool operator ==(DbTable x, DbTable y)
        {
            return x.Equals(y);
        }

        public static bool operator !=(DbTable x, DbTable y)
        {
            return !(x.Equals(y));
        }
    }
    */
}

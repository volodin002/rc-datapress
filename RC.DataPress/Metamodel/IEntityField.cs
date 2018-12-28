using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace RC.DataPress.Metamodel
{
    public interface IEntityField
    {
        string Name { get; }
        string ColumnName { get; set; }

        Type ValueType { get; }
        IEntityType EntityType { get; }

        MemberInfo MemberInfo { get; }

        Type MemberType { get; }

        bool isNullableType { get; }

        bool isRequired { get; set; }

        bool isNotMapped { get; set; }

        IEntityType ValueEntityType { get; }

        bool isCollection { get; }

        bool isSet { get; }
        
        /// <summary>
        /// Make sense only for collections.
        /// If set to TRUE, separate cache Dictionary with parent Id keys will be created for result mapping.
        /// </summary>
        bool isUniqueByKey { get; set; }
        
        bool isProperty { get; }

        bool isDbGenerated { get; set; }

        bool isKey { get; }

        bool isPrimitive { get; }

        int Size { get; set; }
    }
}

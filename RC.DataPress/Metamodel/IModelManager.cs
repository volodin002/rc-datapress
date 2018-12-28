using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace RC.DataPress.Metamodel
{
    public interface IModelManager
    {
        IEntityType Entity(Type type);
        IEntityType Entity<T>();

        IEnumerable<IEntityType> Entities();

        IEnumerable<IEntityType> getDerivedEntityTypes(Type type);

        void ResetEntity(Type type);
        void ResetEntity<T>();

        Func<Type, IEnumerable<FieldInfo>> EntityFieldsGetter { get; set; }
        Func<Type, IEnumerable<PropertyInfo>> EntityPropertiesGetter { get; set; }

        Func<int, T> EntityFactory<T>();

        Delegate EntityFactory(Type type);
    }
}

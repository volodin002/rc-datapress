using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace RC.DataPress.Metamodel
{
    class ModelManagerImpl : IModelManager
    {
        private ConcurrentDictionary<Type, IEntityType> _entityCash;
        private ConcurrentDictionary<Type, Delegate> _entityFactoryCash;

        private Func<Type, IEnumerable<FieldInfo>> _entityFieldsGetter;
        private Func<Type, IEnumerable<PropertyInfo>> _entityPropertiesGetter;

        public ModelManagerImpl()
        {
            _entityCash = new ConcurrentDictionary<Type, IEntityType>();
            _entityFactoryCash = new ConcurrentDictionary<Type, Delegate>();

            _entityFieldsGetter = t => t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            _entityPropertiesGetter = t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

        public IEnumerable<IEntityType> Entities()
        {
            return _entityCash.Values;
        }

        public IEntityType Entity(Type type)
        {
            return _entityCash.GetOrAdd(type, t => new EnityTypeImpl(this, t));
        }

        public IEntityType Entity<T>()
        {
            return Entity(typeof(T));
        }

        public IEnumerable<IEntityType> getDerivedEntityTypes(Type type)
        {
            var entity = Entity(type);

            // Level-order traversal
            var queue = new Queue<IEntityType>();
            while(true)
            {                
                foreach (var e in _entityCash.Values.Where(e => e.BaseEntity == entity))
                    queue.Enqueue(e);

                if (queue.Count == 0) break;
                entity = queue.Dequeue();

                yield return entity;
            }
            
            /*
            var stack = new Stack<IEntityType>(_entityCash.Values.Where(e => e.BaseEntity == entity));
           
            while (stack.TryPop(out var derivedEntity))
            {
                yield return derivedEntity;

                entity = derivedEntity;
                foreach(var e in _entityCash.Values.Where(e => e.BaseEntity == entity))
                    stack.Push(e);
            }
            */
        }

        public void ResetEntity(Type type)
        {
            _entityCash.TryRemove(type, out _);
            _entityFactoryCash.TryRemove(type, out _);
        }

        public void ResetEntity<T>()
        {
            ResetEntity(typeof(T));
        }

        public Func<int, T> EntityFactory<T>()
        {
            return (Func<int, T>)EntityFactory(typeof(T));
        }

        public Delegate EntityFactory(Type type)
        {
            return _entityFactoryCash.GetOrAdd(type, t => Emit.ObjectFactory.EntityFactory(this, t));
        }

        public Func<Type, IEnumerable<FieldInfo>> EntityFieldsGetter { get => _entityFieldsGetter; set => _entityFieldsGetter = value; }
        public Func<Type, IEnumerable<PropertyInfo>> EntityPropertiesGetter { get => _entityPropertiesGetter; set => _entityPropertiesGetter = value; }
    }
}

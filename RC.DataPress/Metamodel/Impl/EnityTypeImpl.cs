using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.ComponentModel.DataAnnotations.Schema;

namespace RC.DataPress.Metamodel
{
    class EnityTypeImpl : IEntityType
    {
        private DbTable _table;
        private Type _entityClass;
        private string _name;
        private IModelManager _modelManager;
        private IEntityType _baseEntity;
        private bool _isAbstract;
        private int _descriminator;
        IEntityField _keyField;

        //private string _tableName;
        //private string _tableSchema;

        private Dictionary<string, IEntityField> _fields;

        public EnityTypeImpl(IModelManager modelManager, Type type)
        {
            _modelManager = modelManager;
            _name = type.Name;
            _entityClass = type;
            _fields = new Dictionary<string, IEntityField>();

            foreach (var entityField in modelManager
                .EntityFieldsGetter(type)
                .Select(f => new EntityFieldImpl(modelManager, this, f)))
            {
                _fields.Add(entityField.Name, entityField);
            }

            foreach (var entityField in modelManager
                .EntityPropertiesGetter(type)
                .Select(p => new EntityFieldImpl(modelManager, this, p)))
            {
                _fields.Add(entityField.Name, entityField);
            }

            _keyField = _fields.Values.SingleOrDefault(f => f.isKey);

            var tablleAttribute = type.GetCustomAttribute<TableAttribute>(false);
            if (tablleAttribute != null)
            {
                _table = new DbTable(tablleAttribute.Name, tablleAttribute.Schema);
                //_tableName = tablleAttribute.Name;
                //_tableSchema = tablleAttribute.Schema;
            }

            var baseType = type.BaseType;
            if (baseType != null && baseType != typeof(object))
            {
                _baseEntity = modelManager.Entity(baseType);
            }
            
        }

        public string Name => _name;

        public Type EntityClass => _entityClass;


        public IEntityType BaseEntity => _baseEntity;

        public bool isAbstract { get => _isAbstract; set => _isAbstract = value; }

        public IEntityField KeyField => _keyField != null ? _keyField : _baseEntity?.KeyField;

        public IModelManager ModelManager => _modelManager;

        public DbTable Table => _table ?? _baseEntity?.Table;

        public int Descriminator { get => _descriminator; set => _descriminator = value; }

        //public string TableName { get => _tableName; set => _tableName = value; }
        //public string TableSchema { get => _tableSchema; set => _tableSchema = value; }
        //public (string name, string schema) Table { get => (_tableName, _tableSchema); set => (_tableName, _tableSchema) = value; }

        public IEntityField getField(string fieldName)
        {
            if (_fields.TryGetValue(fieldName, out var field))
                return field;

            if (_baseEntity != null)
                return _baseEntity.getField(fieldName);

            return null;
        }

        public IEntityField getFieldByColumn(string columnName)
        {
            var field = _fields.Values
                .FirstOrDefault(f => f.ColumnName == columnName);

            if (field != null) return field;

            if (_baseEntity != null)
                return _baseEntity.getFieldByColumn(columnName);

            return null;
        }

        public IEnumerable<IEntityField> getFields()
        {
            if (_baseEntity == null) return _fields.Values;

            return _baseEntity.getFields().Concat(_fields.Values);
        }

        public void setKeyField(string fieldName)
        {
            if (_fields.TryGetValue(fieldName, out var field))
                _keyField = field;
        }

        /*
        public ref readonly DbTable Table => ref _table;
        public void setTable(in DbTable table)
        {
            _table = table;
        }
        */
        public IEnumerable<IEntityType> getDerivedEntityTypes()
        {
           return _modelManager.getDerivedEntityTypes(_entityClass);
        }

        public void setTable(DbTable table)
        {
            _table = table;
        }

        public bool isBaseEntity(IEntityType entity)
        {
            var baseEntity = _baseEntity;
            while (baseEntity != null)
            {
                if (baseEntity == entity) return true;
                baseEntity = baseEntity.BaseEntity;
            }

            return false;
        }
    }
}

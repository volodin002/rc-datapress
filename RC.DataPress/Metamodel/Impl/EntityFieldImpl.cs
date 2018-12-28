using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RC.DataPress.Metamodel
{
    class EntityFieldImpl : IEntityField
    {
        private string _name;
        private string _columnName;
        private Type _valueType;
        private IEntityType _entityType;
        private MemberInfo _memberInfo;
        private IEntityField _foreignKey;
        private IEntityField _collectionInverseField;
        private int _Size;

        private Int32 _flags;

        #region CONSTS
        private const Int32 _isRequired     = 0x001;
        private const Int32 _isNullableType = 0x002;
        private const Int32 _isCollection   = 0x004;
        private const Int32 _isUniqueByKey  = 0x008;
        private const Int32 _isNotMapped    = 0x010;
        private const Int32 _isProperty     = 0x020;
        private const Int32 _isDbGenerated  = 0x040;
        private const Int32 _isKey          = 0x080;
        private const Int32 _isPrimitive    = 0x100;
        private const Int32 _isSet          = 0x200;
        #endregion // CONSTS

        public string Name => _name;

        public string ColumnName { get => _columnName; set => _columnName = value; }

        public Type ValueType => _valueType;

        public IEntityType EntityType => _entityType;

        public IEntityType ValueEntityType => isPrimitive ? null : _entityType.ModelManager.Entity(_valueType);

        public bool isNullableType => (_flags & _isNullableType) > 0;

        public bool isRequired {
            get => (_flags & _isRequired) > 0;
            set => _flags = value ? (_flags | _isRequired) : (_flags & ~_isRequired);
        }

        public bool isNotMapped {
            get => (_flags & _isNotMapped) > 0;
            set => _flags = value ? (_flags | _isNotMapped) : (_flags & ~_isNotMapped);
        }


        public bool isCollection => (_flags & _isCollection) > 0;

        public bool isSet => (_flags & _isSet) > 0;

        public bool isUniqueByKey {
            get => (_flags & _isUniqueByKey) > 0;
            set => _flags = value ? (_flags | _isUniqueByKey) : (_flags & ~_isUniqueByKey);
        }

        public bool isProperty => (_flags & _isProperty) > 0;

        public bool isDbGenerated {
            get => (_flags & _isDbGenerated) > 0;
            set => _flags = value ? (_flags | _isDbGenerated) : (_flags & ~_isDbGenerated);
        }

        public bool isKey => (_flags & _isKey) > 0;

        public bool isPrimitive => (_flags & _isPrimitive) > 0;

        public int Size { get => _Size; set => _Size = value; }

        public MemberInfo MemberInfo => _memberInfo;

        public Type MemberType => isProperty ? ((PropertyInfo)_memberInfo).PropertyType : ((FieldInfo)_memberInfo).FieldType;

        public IEntityField ForeignKey { get => _foreignKey; set => _foreignKey = value; }

        private void setIsNullableType(bool value) => _flags = value ? (_flags | _isNotMapped) : (_flags & ~_isNotMapped);
        private void setIsCollection(bool value) => _flags = value ? (_flags | _isCollection) : (_flags & ~_isCollection);
        private void setIsKey(bool value) => _flags = value ? (_flags | _isKey) : (_flags & ~_isKey);
        private void setIsPrimitive(bool value) => _flags = value ? (_flags | _isPrimitive) : (_flags & ~_isPrimitive);

        private void setIsProperty(bool value) => _flags = value ? (_flags | _isProperty) : (_flags & ~_isProperty);

        private void setIsSet(bool value) => _flags = value ? (_flags | _isSet) : (_flags & ~_isSet);

        public EntityFieldImpl(IModelManager modelManager, IEntityType entityType, PropertyInfo prop)
        {
            Init(modelManager, entityType, prop, prop.PropertyType);
            setIsProperty(true);
        }

        public EntityFieldImpl(IModelManager modelManager, IEntityType entityType, FieldInfo field)
        {
            Init(modelManager, entityType, field, field.FieldType);
        }

        private void Init(IModelManager modelManager, IEntityType entityType, MemberInfo prop, Type type)
        {
            _entityType = entityType;
            _name = prop.Name;
            _columnName = _name;
            _memberInfo = prop;

            bool is_collection = type != typeof(string) &&
                typeof(IEnumerable).IsAssignableFrom(type);

            if (is_collection)
            {
                setIsCollection(true);

                var types = type.GetGenericArguments();
                if (types.Length != 1)
                    throw new NotSupportedException(type.FullName + " is not supported as Entity field or property type");
                _valueType = types[0];

                bool is_set = typeof(ISet<>).IsAssignableFrom(type);
                isUniqueByKey = !is_set;
                setIsSet(is_set);
            }
            else
                _valueType = type;

            setIsNullableType(Helper.IsNullableType(_valueType));

            if (isNullableType)
                _valueType = Helper.GetNonNullableType(_valueType);

            if (Helper.IsPrimitiveType(type))
                setIsPrimitive(true);

            foreach (var attr in prop.GetCustomAttributes())
            {
                switch (attr)
                {
                    case KeyAttribute a:
                        setIsKey(true);
                        break;
                    case DatabaseGeneratedAttribute a:
                        isDbGenerated = a.DatabaseGeneratedOption != DatabaseGeneratedOption.None;
                        break;
                    case RequiredAttribute a:
                        isRequired = true;
                        break;
                    case ColumnAttribute a:
                        _columnName = a.Name;
                        break;
                    case NotMappedAttribute a:
                        isNotMapped = true;
                        break;
                    case StringLengthAttribute a:
                        _Size = a.MaximumLength;
                        break;
                   
                }
            }
        }

        public IEntityField CollectionInverseField
        {
            get
            {
                if (isPrimitive && !isCollection) return null;

                if (_collectionInverseField == null)
                {
                    var attr = _memberInfo.GetCustomAttribute<InversePropertyAttribute>();
                    if (attr != null)
                    {
                        _collectionInverseField = ValueEntityType.getField(attr.Property);
                    }
                }

                return _collectionInverseField;
            }

            set { _collectionInverseField = value; }
        }

    }
}

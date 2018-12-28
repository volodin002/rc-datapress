using System;
using System.Collections.Generic;
using System.Text;

namespace RC.DataPress.Metamodel
{
    public interface IEntityType
    {
        string Name { get; }
        Type EntityClass { get; }

        //string TableName { get; set; }
        //string TableSchema { get; set; }

        
        DbTable Table { get; }

        void setTable(DbTable table);
        
        IEntityType BaseEntity { get; }

        IEntityField getField(string fieldName);
        IEntityField getFieldByColumn(string columnName);

        IEnumerable<IEntityField> getFields();

        bool isAbstract { get; set; }

        int Descriminator { get; set; }

        void setKeyField(string field);

        IEntityField KeyField { get; }


        IModelManager ModelManager { get; }

        IEnumerable<IEntityType> getDerivedEntityTypes();

        bool isBaseEntity(IEntityType entity);

    }


}

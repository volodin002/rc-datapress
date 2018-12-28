using RC.DataPress.Metamodel;
using RC.Task.Core.DB;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;

namespace RC.DataPress.Test
{
    class TaskCoreTest
    {
        IModelManager manager;
        DbContext<SqlConnection> cntx;

        public TaskCoreTest()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["RC.GRDB.Connection"].ConnectionString;
            manager = DbContext<SqlConnection>.CreateModelManager();
            cntx = new DbContext<SqlConnection>(manager, connectionString);
        }

        public void LoadTasks()
        {
            var taskEntity = manager.Entity<DbTask>();
            var actionEntity = manager.Entity<DbTaskAction>();
            var logEntity = manager.Entity<DbTaskLog>();

            string sql = @"
select top(1) t.Id, t.Name
from dbo.Task t
";
            Func<DbDataReader, IModelManager, IList<DbTask>> mapper = null;
            var tasks = cntx.GetResultList<DbTask>(sql, ref mapper);
            sql = @"
select t.Id, t.Name
from dbo.Task t
";
            tasks = cntx.GetResultList<DbTask>(sql, ref mapper);
        }
    }
}

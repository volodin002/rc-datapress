using System;
using System.Collections.Generic;
using System.Text;

namespace RC.Task.Common
{
    public interface ITaskAction
    {
        void Do(TaskContext cntx);
    }
}

using RC.Task.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RC.Task.Core
{
    public class TaskAction : TaskItem
    {
        public string Title;
        public string Desc;

        public Type Type;

        public ITaskAction Action;
    }
}

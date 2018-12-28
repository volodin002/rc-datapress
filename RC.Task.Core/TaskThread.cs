using System;
using System.Collections.Generic;
using System.Text;

namespace RC.Task.Core
{
    class TaskThread
    {
        Task _task;
        public TaskThread(Task task)
        {
            _task = task;
        }

        public void Poll()
        {

        }

    }
}

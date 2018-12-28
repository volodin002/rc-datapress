using RC.Task.Common;
using RC.Task.Core.DB;
using System;
using System.Collections.Generic;
using System.Text;

namespace RC.Task.Core
{
    class TaskDbLogProvider : ILogProvider
    {
        public void Write(LogLevel type, string message)
        {
            var log = new DbTaskLog() {
                Level = (byte)type,
                Created = DateTime.UtcNow,
                Message = message
            };
        }

        public void Write(LogLevel type, string message, Exception ex)
        {
            var log = new DbTaskLog()
            {
                Level = (byte)type,
                Created = DateTime.UtcNow,
                Message = message,
                Error = ex.ToString()
            };

        }

        public void Write(LogLevel type, TaskItem task, int threadId, string message)
        {
            var log = new DbTaskLog()
            {
                Level = (byte)type,
                TaskId = task.Id,
                Thread = threadId,
                Created = DateTime.UtcNow,
                Message = message,
            };
        }

        public void Write(LogLevel type, TaskItem task, int threadId, string message, Exception ex)
        {
            var log = new DbTaskLog()
            {
                Level = (byte)type,
                TaskId = task.Id,
                Thread = threadId,
                Created = DateTime.UtcNow,
                Message = message,
                Error = ex.ToString()
            };
        }

        public void Write(LogLevel type, TaskItem task, TaskItem action, int threadId, string message)
        {
            var log = new DbTaskLog()
            {
                Level = (byte)type,
                TaskId = task.Id,
                ActionId = action.Id,
                Thread = threadId,
                Created = DateTime.UtcNow,
                Message = message,
            };
        }

        public void Write(LogLevel type, TaskItem task, TaskItem action, int threadId, string message, Exception ex)
        {
            var log = new DbTaskLog()
            {
                Level = (byte)type,
                TaskId = task.Id,
                ActionId = action.Id,
                Thread = threadId,
                Created = DateTime.UtcNow,
                Message = message,
                Error = ex.ToString()
            };
        }
    }
}

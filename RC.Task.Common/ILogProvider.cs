using System;

namespace RC.Task.Common
{
    public enum LogLevel : int
    {
        Fatal = 0,
        Error = 1,
        Warning = 2,
        Information = 3,
        Debug  = 4,
        Trace = 5
    }
    public interface ILogProvider
    {
        void Write(LogLevel type, string message);
        void Write(LogLevel type,  string message, Exception ex);

        void Write(LogLevel type, TaskItem task, int threadId, string message);
        void Write(LogLevel type, TaskItem task, int threadId, string message, Exception ex);

        void Write(LogLevel type, TaskItem task, TaskItem action, int threadId, string message);
        void Write(LogLevel type, TaskItem task, TaskItem action, int threadId, string message, Exception ex);
    }
}

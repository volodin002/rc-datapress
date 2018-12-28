using RC.Task.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RC.Task.Core
{
    static class Config
    {
        public static string ConnectionString;

        public static ILogProvider[] LogProviders;

        internal static void Log(LogLevel level, string message, TaskItem task, int threadIndx, TaskItem action)
        {
            int cnt = LogProviders.Length;
            if (action == null)
            {
                for (int i = 0; i < cnt; i++)
                    Config.LogProviders[i].Write(level, task, threadIndx, message);
            }
            else
            {
                for (int i = 0; i < cnt; i++)
                    Config.LogProviders[i].Write(level, task, action, threadIndx, message);
            }
        }

        internal static void Log(LogLevel level, string message, TaskItem task, int threadIndx, TaskItem action, Exception ex)
        {
            int cnt = Config.LogProviders.Length;
            if (action == null)
            {
                for (int i = 0; i < cnt; i++)
                    Config.LogProviders[i].Write(level, task, threadIndx, message, ex);
            }
            else
            {
                for (int i = 0; i < cnt; i++)
                    Config.LogProviders[i].Write(level, task, action, threadIndx, message, ex);
            }
        }
    }
}

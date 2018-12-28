using Newtonsoft.Json;
using RC.Task.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RC.Task.Core
{
    class TaskContextImpl : TaskContext
    {
        private Task _task;
        private TaskAction _action;

        public readonly Thread Thread;
        public readonly int ThreadIndx;
        public TaskContextImpl(Task task, Thread thread, int threadIndx) : base(task.Parameters)
        {
            _task = task;
            Thread = thread;
            ThreadIndx = threadIndx;
        }

        public void BeginAction(TaskAction action)
        {
            _action = action;
        }

        public void EndAction()
        {
            _action = null;
        }

        public void CancelOnStop()
        {
            _isDone |= 8;
        }

        public void OnStop()
        {
            if (_waitHandle != null) _waitHandle.Set();

            Thread.Join();
        }

        #region Logging
        public override string ToStringForTrace()
        {
            return JsonConvert.SerializeObject((TaskContext)this);
        }

        public override void Trace(string message)
        {
            if (_task.LogLevel < (int)LogLevel.Trace) return;

            Config.Log(LogLevel.Trace, message, _task, ThreadIndx, _action);
        }

        public override void Trace(Func<string> message)
        {
            if (_task.LogLevel < (int)LogLevel.Trace) return;

            Config.Log(LogLevel.Trace, message(), _task, ThreadIndx, _action);
        }

        public override void Debug(string message)
        {
            if (_task.LogLevel < (int)LogLevel.Debug) return;

            Config.Log(LogLevel.Debug, message, _task, ThreadIndx, _action);
        }

        public override void Debug(Func<string> message)
        {
            if (_task.LogLevel < (int)LogLevel.Debug) return;

            Config.Log(LogLevel.Debug, message(), _task, ThreadIndx, _action);
        }

        public override void Info(string message)
        {
            if (_task.LogLevel < (int)LogLevel.Information) return;

            Config.Log(LogLevel.Information, message, _task, ThreadIndx, _action);
        }

        public override void Info(Func<string> message)
        {
            if (_task.LogLevel < (int)LogLevel.Information) return;

            Config.Log(LogLevel.Information, message(), _task, ThreadIndx, _action);
        }

        public override void Warn(string message)
        {
            if (_task.LogLevel < (int)LogLevel.Warning) return;

            Config.Log(LogLevel.Warning, message, _task, ThreadIndx, _action);
        }

        public override void Warn(Func<string> message)
        {
            if (_task.LogLevel < (int)LogLevel.Warning) return;

            Config.Log(LogLevel.Warning, message(), _task, ThreadIndx, _action);
        }

        public override void Error(string message)
        {
            Config.Log(LogLevel.Error, message, _task, ThreadIndx, _action);
        }

        public override void Error(Func<string> message)
        {
            if (_task.LogLevel < (int)LogLevel.Error) return;

            Config.Log(LogLevel.Error, message(), _task, ThreadIndx, _action);
        }

        public override void Fatal(string message)
        {
            Config.Log(LogLevel.Fatal, message, _task, ThreadIndx, _action);
        }

        public override void Fatal(Func<string> message)
        {
            Config.Log(LogLevel.Fatal, message(), _task, ThreadIndx, _action);
        }

        public override void Trace(string message, Exception exception)
        {
            if (_task.LogLevel < (int)LogLevel.Trace) return;

            Config.Log(LogLevel.Trace, message, _task, ThreadIndx, _action, exception);
        }

        public override void Trace(Func<string> message, Exception exception)
        {
            if (_task.LogLevel < (int)LogLevel.Trace) return;

            Config.Log(LogLevel.Trace, message(), _task, ThreadIndx, _action, exception);
        }

        public override void Debug(string message, Exception exception)
        {
            if (_task.LogLevel < (int)LogLevel.Debug) return;

            Config.Log(LogLevel.Debug, message, _task, ThreadIndx, _action, exception);
        }

        public override void Debug(Func<string> message, Exception exception)
        {
            if (_task.LogLevel < (int)LogLevel.Debug) return;

            Config.Log(LogLevel.Debug, message(), _task, ThreadIndx, _action, exception);
        }

        public override void Info(string message, Exception exception)
        {
            if (_task.LogLevel < (int)LogLevel.Information) return;

            Config.Log(LogLevel.Information, message, _task, ThreadIndx, _action, exception);
        }

        public override void Info(Func<string> message, Exception exception)
        {
            if (_task.LogLevel < (int)LogLevel.Information) return;

            Config.Log(LogLevel.Information, message(), _task, ThreadIndx, _action, exception);
        }

        public override void Warn(string message, Exception exception)
        {
            if (_task.LogLevel < (int)LogLevel.Warning) return;

            Config.Log(LogLevel.Warning, message, _task, ThreadIndx, _action, exception);
        }

        public override void Warn(Func<string> message, Exception exception)
        {
            if (_task.LogLevel < (int)LogLevel.Warning) return;

            Config.Log(LogLevel.Warning, message(), _task, ThreadIndx, _action, exception);
        }

        public override void Error(string message, Exception exception)
        {
            if (_task.LogLevel < (int)LogLevel.Error) return;

            Config.Log(LogLevel.Error, message, _task, ThreadIndx, _action, exception);
        }

        public override void Error(Func<string> message, Exception exception)
        {
            if (_task.LogLevel < (int)LogLevel.Error) return;

            Config.Log(LogLevel.Error, message(), _task, ThreadIndx, _action, exception);
        }
        public override void Fatal(string message, Exception exception)
        {
            Config.Log(LogLevel.Fatal, message, _task, ThreadIndx, _action, exception);
        }

        public override void Fatal(Func<string> message, Exception exception)
        {
            Config.Log(LogLevel.Fatal, message(), _task, ThreadIndx, _action, exception);
        }

        //private void Log(LogLevel level, string message)
        //{
        //    int cnt = Config.LogProviders.Length;
        //    if (_action == null)
        //    {
        //        for (int i = 0; i < cnt; i++)
        //            Config.LogProviders[i].Write(level, _task, ThreadIndx, message);
        //    }
        //    else
        //    {
        //        for (int i = 0; i < cnt; i++)
        //            Config.LogProviders[i].Write(level, _task, _action, ThreadIndx, message);
        //    }
        //}
        //private void Log(LogLevel level, string message, Exception ex)
        //{
        //    int cnt = Config.LogProviders.Length;
        //    if (_action == null)
        //    {
        //        for (int i = 0; i < cnt; i++)
        //            Config.LogProviders[i].Write(level, _task, ThreadIndx, message, ex);
        //    }
        //    else
        //    {
        //        for (int i = 0; i < cnt; i++)
        //            Config.LogProviders[i].Write(level, _task, _action, ThreadIndx, message, ex);
        //    }
        //}

        #endregion // Logging
    }
}

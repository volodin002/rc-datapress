using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RC.Task.Common
{
    public abstract class TaskContext : Dictionary<string, object>, IDisposable
    {
        protected int _isDone;
        protected ManualResetEvent _waitHandle;

        protected Dictionary<string, object> _taskParamteres;

        public TaskContext(Dictionary<string, object> taskParamteres)
        {
            _taskParamteres = taskParamteres;
        }
        public bool TryGetValue<T>(string name, out T val)
        {
            if (TryGetValue(name, out var value))
            {
                val = value != null ? (T)value : default(T);
                return true;
            }

            val = default(T);
            return false;
        }

        public T GetValue<T>(string name)
        {
            if (TryGetValue(name, out var value))
                return value != null ? (T)value : default(T);

            return default(T);
        }

        public T GetValue<T>(string name, T defaultValue)
        {
            if (TryGetValue(name, out var value))
                return value != null ? (T)value : defaultValue;

            return defaultValue;
        }

        public T GetTaskParameterValue<T>(string name)
        {
            if (_taskParamteres.TryGetValue(name, out var value))
                return value != null ? (T)value : default(T);

            return default(T);
        }

        public T GetTaskParameterValue<T>(string name, T defaultValue)
        {
            if (_taskParamteres.TryGetValue(name, out var value))
                return value != null ? (T)value : defaultValue;

            return defaultValue;
        }

        public void DoneSuccessfully()
        {
            _isDone |= 1;
        }
        public void Cancel()
        {
            _isDone |= 2;
        }

        public void CancelDueToError()
        {
            _isDone |= 4;
        }

        public bool IsDone() => _isDone > 0;


        public WaitHandle WaitHandle() => _waitHandle ?? (_waitHandle = new ManualResetEvent(false));


        #region Logging
        public abstract void Trace(string message);

        public abstract void Trace(Func<string> message);
        public abstract void Debug(string message);
        public abstract void Debug(Func<string> message);
        public abstract void Info(string message);

        public abstract void Info(Func<string> message);
        public abstract void Warn(string message);
        public abstract void Warn(Func<string> message);
        public abstract void Error(string message);
        public abstract void Error(Func<string> message);
        public abstract void Fatal(string message);
        public abstract void Fatal(Func<string> message);

        public abstract void Trace(string message, Exception exception);
        public abstract void Trace(Func<string> message, Exception exception);
        public abstract void Debug(string message, Exception exception);
        public abstract void Debug(Func<string> message, Exception exception);
        public abstract void Info(string message, Exception exception);
        public abstract void Info(Func<string> message, Exception exception);
        public abstract void Warn(string message, Exception exception);
        public abstract void Warn(Func<string> message, Exception exception);
        public abstract void Error(string message, Exception exception);
        public abstract void Error(Func<string> message, Exception exception);
        public abstract void Fatal(string message, Exception exception);
        public abstract void Fatal(Func<string> message, Exception exception);

        public abstract string ToStringForTrace();

        public void Dispose()
        {
            var waitHandle = _waitHandle;
            if (waitHandle != null)
                _waitHandle.Dispose();
        }

        #endregion // Logging
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RC.Task.Core
{
    public class MainThreadLoop 
    {
        private static ManualResetEvent waitEvent;
        private static Thread mainThread = null;

        public static int Rate = 1000;

        private static List<Task> _tasks;
        public void Start(IEnumerable<Task> tasks)
        {
            _tasks = new List<Task>();
            foreach(var task in tasks)
            {
                task.OnStart();
                _tasks.Add(task);
            }

            waitEvent = new ManualResetEvent(false);

            mainThread = new Thread(Loop);
            mainThread.IsBackground = true;
            mainThread.Start();
        }

        public static void Stop()
        {
            if (mainThread == null) return;

            waitEvent.Set();

            if (mainThread.Join(15000))
            {
                if (waitEvent != null)
                {
                    waitEvent.Dispose();
                    waitEvent = null;
                }
                mainThread = null;
            }
            
        }

        private static void Loop()
        {
            int cnt;
            try
            {
                while (!waitEvent.WaitOne(Rate, false))
                {
                    try
                    {
                        cnt = _tasks.Count;
                        for (int i = 0; i < cnt; i++)
                        {
                            _tasks[i].OnPoll();
                        }
                    }
                    catch (Exception ex)
                    {
                        Config.Log(Common.LogLevel.Error, ex.Message, null, 0, null, ex);
                    }
                }

                cnt = _tasks.Count;
                for (int i = 0; i < cnt; i++)
                {
                    _tasks[i].OnStop();
                }

            }
            catch (Exception ex)
            {
                Config.Log(Common.LogLevel.Fatal, ex.Message, null, 0, null, ex);
                throw;
            }
        }
    }
}

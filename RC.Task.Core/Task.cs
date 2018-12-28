using System;
using System.Collections.Generic;
using System.Linq;
using RC.Task.Common;
using System.Threading;

namespace RC.Task.Core
{
    public class Task : TaskItem
    {
        public string Title;
        public string Desc;

        public TaskAction[] Actions;

        public Dictionary<string, object> Parameters;

        public bool IsFixedRate;

        public int Rate; // in milliseconds

        public int Delay; // in milliseconds

        public int MinThreads;

        public int MaxThreads;

        public int LogLevel;

        private long _time;

        private TaskContextImpl[] _contexts;

        public void OnStart()
        {
            _time = DateTime.UtcNow.Ticks + Delay;
            _contexts = new TaskContextImpl[MaxThreads];
        }

        public void OnStop()
        {
            for (int i = 0; i < MaxThreads; i++)
            {
                var cntx = _contexts[i];
                cntx.CancelOnStop();
                cntx.OnStop();
            }
        }

        public void OnPoll()
        {
            if (DateTime.UtcNow.Ticks < _time) return;

            if (IsFixedRate) _time += Rate;

            int aliveThreadsCount = _contexts.Count(x => x != null);
            if (aliveThreadsCount >= MaxThreads) return;

            if (!IsFixedRate) _time += Rate;

            for (int i = 0; i < MaxThreads; i++)
            {
                var cntx = _contexts[i];
                if (cntx != null) continue;

                var thread = new Thread(DoTask) { IsBackground = true };
                cntx = new TaskContextImpl(this, thread, i);
                _contexts[i] = cntx;
                thread.Start(cntx);

                if(++aliveThreadsCount >= MinThreads) continue;
            }

        }

        private void DoTask(object context)
        {
            var cntx = (TaskContextImpl)context;
            try
            {
                cntx.Debug("Start task");
                cntx.Trace(() => cntx.ToStringForTrace());


                int cnt = Actions.Length;
                for (int i = 0; i < cnt; i++)
                {
                    var action = Actions[i];

                    cntx.BeginAction(action);
                    cntx.Debug("Start action");

                    action.Action.Do(cntx);

                    cntx.Debug("End action");
                    cntx.Trace(() => cntx.ToStringForTrace());

                    if (cntx.IsDone())
                    {
                        cntx.Debug("Done");
                        break;
                    }
                    cntx.EndAction();
                }


                cntx.Trace(() => cntx.ToStringForTrace());
                cntx.Debug("End task");
            }
            catch (Exception ex)
            {
                cntx.Trace(() => cntx.ToStringForTrace());
                cntx.Error(ex.Message, ex);
            }
            finally
            {
                cntx.Dispose();
                _contexts[cntx.ThreadIndx] = null;
            }
        }

    }
}

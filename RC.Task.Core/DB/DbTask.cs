using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RC.Task.Core.DB
{
    public class DbTask
    {
        [Key]
        public int Id;
        public string Name;
        public string Title;
        public string Desc;

        public string Module;
        public string Type;

        public string ParametersJson;

        public bool IsFixedRate;

        public int Rate;   // in milliseconds

        public int Delay; // in milliseconds

        public int MinThreads;

        public int MaxThreads;

        public byte LogLevel;

        public List<DbTaskAction> Actions;

        public List<DbTaskLog> Logs;
    }
}

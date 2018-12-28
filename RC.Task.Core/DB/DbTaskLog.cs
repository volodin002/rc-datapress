using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RC.Task.Core.DB
{
    public class DbTaskLog
    {
        [Key]
        public long Id;
        public int? TaskId;
        public int? ActionId;
        public int? Thread;

        public byte Level;

        public DateTime Created;

        public string Message;

        public string Error;
    }
}

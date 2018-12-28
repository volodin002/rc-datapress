using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RC.Task.Core.DB
{
    public class DbTaskAction
    {
        [Key]
        public int Id;
        public string Name;
        public string Title;
        public string Desc;

        public string Module;
        public string Type;

        public string ParametersJson;

        public int Order;
    }
}

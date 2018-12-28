using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace RC.Task.Core.DB
{
    public class DbTag
    {
        [Key]
        public int Id;
        public string Name;

        public string Description;
    }
}

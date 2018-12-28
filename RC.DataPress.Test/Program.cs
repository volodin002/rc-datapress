using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Text;

namespace RC.DataPress.Test
{
    enum test_enum
    {
        case1 = 1,
        case2 = 2
    }
    class Program
    {
        static void Main(string[] args)
        {

            //var array = new Collections.ResizableArrayWithKeySet<int, Task.Common.TaskItem>();
            //int id = 100;
            //Task.Common.TaskItem taskItem = null;
            //array.TryGet(id, ref taskItem);

            //Func<int, Task.Common.TaskItem> f = x => new Task.Common.TaskItem();

            //Delegate dlg = f;

            //Test1();
            //decimal d = -92233720368547800.000000000000M;

            //decimal d; // decimal.Parse("-9223372036854780011111223.000002211111");
            //decimal.TryParse("-9223372036854780011111223.000002211111", out d);

            //var taskItem = f(0);

            var t = typeof(string);
            //DateTime i = DateTime.MinValue; //default(DateTime);
            ///DateTime dt = DateTime.MinValue;
            //var d = Decimal.Zero;
            //ref decimal rd = ref d;

            //char ch = default(char);

            //Guid g = default(Guid);
            //Guid g1 = Guid.Empty;
            int i = 2;
            if (i > (int)test_enum.case1)
            {
                i = 3;
            }

            int[] arr = { 1, 2, 3 };
            foreach (var v in arr)
            {
                Console.Write(v);
            }

            var task = new Task.Core.Task();
            var cache = new Dictionary<int, string>();

            cache.TryGetValue(1, out task.Name);

            /*
            var e = test_enum.case1;
            switch (e)
            {
                case test_enum.case1:
                    Console.WriteLine("100");break;
                case test_enum.case2:
                    Console.WriteLine("200"); break;
                default:
                    Console.WriteLine("default"); break;
            }
            */
            //DateTime i = DateTime.Now;
            //DateTime j = DateTime.Now;

            //if (i == j)
            //    Console.WriteLine("Hello World!");

            //Test();

            //StringBuilder test = new StringBuilder("Hello world");
            //var t = new RefTest(ref test);
            //t.Do();
            //Console.WriteLine(test.ToString());
            //Console.WriteLine(t.Test);

            var test = new TaskCoreTest();
            test.LoadTasks();

            Console.ReadKey();
        }

        static void Test()
        {
            var connectionStr = ConfigurationManager.ConnectionStrings["RC.GRDB.Connection"].ConnectionString;
            using (var con = new SqlConnection(connectionStr))
            {
                using (var cmd = new SqlCommand("Select Id from Cp.Party", con))
                {
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        while(r.Read())
                        {
                            Console.WriteLine("ID:" + r.GetInt32(0).ToString());
                        }
                    }
                }
            }
        }

        static void Test1()
        {
            var connectionStr = ConfigurationManager.ConnectionStrings["RC.GRDB"].ConnectionString;
            using (var con = new SqlConnection(connectionStr))
            {
                using (var cmd = new SqlCommand("Select Mid from [Prices].[Log_wrong]", con))
                //
                //using (var cmd = new SqlCommand("declare @dt datetime =GetDate();exec Processing.GetLogPricesForInterpretation @ProcessingDate=@dt", con))
                {
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                    {
                        //using (var stream = File.CreateText("def.xml"))
                        //{
                        //    r.GetSchemaTable().WriteXml(stream);
                        //    stream.Flush();
                        //    stream.Close();
                        //}
                            
                        while (r.Read())
                        {
                            /*
                            object val = r.GetValue(0);
                            Console.WriteLine("MID(object):" + val.ToString());

                            decimal d = (decimal)val;
                            */
                            System.Data.SqlTypes.SqlDecimal sd = r.GetSqlDecimal(0);
                            Console.WriteLine("MID(sql decimal):" + sd.ToString());
                            Console.WriteLine($"Precision: {sd.Precision}, Scale: {sd.Scale}");

                            sd  = System.Data.SqlTypes.SqlDecimal.ConvertToPrecScale(sd, 28, 10);
                            Console.WriteLine("MID(converted sql decimal):" + sd.ToString());



                            decimal d = sd.Value;
                            Console.WriteLine("MID(decimal):" + d.ToString());

                            //double d = sd.ToDouble();
                            //Console.WriteLine("MID(double):" + d.ToString());

                            //Console.WriteLine("MID(bin):" + String.Join(',', sd.Data));


                            //var money = sd.ToSqlMoney();
                            //Console.WriteLine("MID(sql money):" + money.ToString());

                            //var de = money.ToDecimal();
                            //Console.WriteLine("MID(decimal):" + de.ToString());

                        }
                    }
                }
            }
        }
    }

//    class RefTest
//    {
//        ref StringBuilder _text;
//        public RefTest(ref StringBuilder test)
//        {
//            _text = test;
//        }

//        public void Do()
//        {
//            _text = new StringBuilder("!!!");
//        }

//        public string Test { get => _text.ToString(); }

//    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using msgp.mc.model;
using System.IO;
using System.Diagnostics;

namespace testwin
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<testobject<testobject2>> listdata = new List<testobject<testobject2>>();
            for (int i = 0; i < 1000; i++)
            {
                testobject<testobject2> testobj = new testobject<testobject2>();
                listdata.Add(testobj);
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var bin=  MessagePack.MessagePackSerializer.Serialize< List<testobject<testobject2>>>(listdata, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            var filename1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mspack.data");
              File.WriteAllBytes(filename1, bin);
            sw.Stop();
            TimeSpan ts = sw.Elapsed;
            Console.WriteLine("DateTime costed for MessagePackSerializer function is: {0}ms", ts.TotalMilliseconds);
             sw.Start();
            var str=  Newtonsoft.Json.JsonConvert.SerializeObject(listdata);           
            var filename2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "json.data");
            File.WriteAllText(filename2, str);
            sw.Stop();
             ts = sw.Elapsed;
            Console.WriteLine("DateTime costed for Newtonsoft.Json function is: {0}ms", ts.TotalMilliseconds);


        }

        private void button2_Click(object sender, EventArgs e)
        {
               var filename1 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mspack.data");
            var bin = File.ReadAllBytes(filename1);
            testobject<testobject2> testobj = MessagePack.MessagePackSerializer.Deserialize<testobject<testobject2>>(bin,MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            MessageBox.Show("");
        }
    }
}

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
using AutoMapper;
using System.Reflection;
using MessagePack;
using System.Collections;

namespace testwin
{
    public partial class Form1 : Form
    {
        private string filename { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mspack.data"); } }
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
                testobj.ft.f1 = i.ToString();
                listdata.Add(testobj);
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var bin=  MessagePack.MessagePackSerializer.Serialize< List<testobject<testobject2>>>(listdata, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
           
            File.WriteAllBytes(filename, bin);
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

            var bin = File.ReadAllBytes(filename);
            List<testobject<object>> testobj = MessagePack.MessagePackSerializer.Deserialize<List<testobject<object>>>(bin, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            var config = new MapperConfiguration(cfg => cfg.CreateMissingTypeMaps = true);
            var mapper = config.CreateMapper();
            var y = mapper.Map<testobject2>(testobj[0].ft);
            var x = (testobj[0].ft as testobject2);

            MessageBox.Show("");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var data = new testobject<testobject2>();
            data.fttype = typeof(int).ToString();
            var bin = MessagePack.MessagePackSerializer.Serialize<testobject<testobject2>>(data, MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            File.WriteAllBytes(filename, bin);
             bin = File.ReadAllBytes(filename);
            testobject<testobject2> testobj = MessagePack.MessagePackSerializer.Deserialize<testobject<testobject2>>(bin, MessagePack.Resolvers.ContractlessStandardResolver.Instance);

            ArrayList t1=new ArrayList() ;
            Hashtable t2 = new Hashtable();
            Console.WriteLine(t1.GetType().ToString());
            Console.WriteLine(t2.GetType().ToString());

            IEnumerable<Assembly> allAssemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies().Select(Assembly.Load);

            Assembly mockAssembly = (from x in allAssemblies where x.FullName.Contains("MessagePack") select x).FirstOrDefault();
            var MethodType = mockAssembly.GetType("MessagePack.MessagePackSerializer");
            var GenericMethod = MethodType.GetMethod("Deserialize",BindingFlags.Static | BindingFlags.Public,null,new Type[] { typeof(byte[]), typeof(IFormatterResolver) },null);
             var genMethod = GenericMethod.MakeGenericMethod(typeof(testobject<testobject2>));
            var obj = genMethod.Invoke(genMethod, new object[] {bin, MessagePack.Resolvers.ContractlessStandardResolver.Instance});

        }
    }
}

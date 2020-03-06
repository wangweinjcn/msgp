using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
namespace Proxy.Comm
{
    public class pCounter
    {
        public string Name;
        public string Memo;
        public perfCategory ownCategory;
        public pCounter(string name, string memo)
        {
            this.Name = name;
            this.Memo = memo;
        }
        public JObject toJson()
        {
         
         return JObject.Parse(Newtonsoft.Json.JsonConvert.SerializeObject(this));
        }
    }
    public class perfCategory
    {
        public    string Name;
        public string Memo;
        public bool isMonoOnly = false;
        [JsonIgnoreAttribute]
        public List<pCounter> allCounter;
        public perfCategory(string name, string memo,bool onlyMono=false)
        {
            this.Name = name;
            this.Memo = memo;
            isMonoOnly = onlyMono;
            allCounter = new List<pCounter>();
        }
        public pCounter addCounter(string name, string memo)
        {
            pCounter pc = new pCounter(name, memo);
            this.allCounter.Add(pc);
            pc.ownCategory = this;
            return pc;
        }
        public pCounter getPCounterByName(string name)
        {
            var z = (from x in this.allCounter where x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) select x).ToList();
            if (z.Count > 0)
                return z.First();
            else
                return null;
        }
    }
   public  class PerfCounterHelper
    {
      public  static List<perfCategory> allPerfCategory;
        static PerfCounterHelper()
        {
            /*
           pcg.addCounter("", "");
           
             */
            allPerfCategory = new List<perfCategory>();
            pCounter pc;
            perfCategory pcg;
            pcg = new perfCategory("Processor", "cpu  info");
           pcg.addCounter("% User Time", "");           
           pcg.addCounter("% Privileged Time", "");           
           pcg.addCounter("% Interrupt Time", "");           
           pcg.addCounter("% DCP Time", "");           
           pcg.addCounter("% Processor Time", "");
            allPerfCategory.Add(pcg);

            pcg = new perfCategory("Process", "process  info");
           pcg.addCounter("% User Time", "");           
           pcg.addCounter("% Privileged Time", "");           
           pcg.addCounter("% Processor Time", "");           
           pcg.addCounter("Thread Count", "");
            pcg.addCounter("Virtual Bytes", "");           
           pcg.addCounter("Working Set", "");           
           pcg.addCounter("Private Bytes", "");
            allPerfCategory.Add(pcg);

            pcg = new perfCategory("Mono Memory", "this category is Mono-specific",true);
           pcg.addCounter("Allocated Objects", "");           
           pcg.addCounter("Total Physical Memory", "Physical memory installed in the machine, in bytes");
            allPerfCategory.Add(pcg);

            pcg = new perfCategory("ASP.NET", "asp.net counter");
           pcg.addCounter("Requests Queued", "");           
           pcg.addCounter("Requests Totale", "");           
           pcg.addCounter("Requests/Sec", "");
            allPerfCategory.Add(pcg);



            pcg = new perfCategory(".NET CLR JIT", ".net counter");
           pcg.addCounter("# of IL Bytes JITted", "");           
           pcg.addCounter("# of IL Methods JITted", "");           
           pcg.addCounter("% Time in JIT", "");           
           pcg.addCounter("IL Bytes Jitted/Sec", "");           
           pcg.addCounter("Standard Jit Failures", "");
            allPerfCategory.Add(pcg);


            pcg = new perfCategory(".NET CLR Exceptions", ".net counter");
           pcg.addCounter("# of Exceps Thrown", "Number of items, 32bit");           
           pcg.addCounter("# of Exceps Thrown/Sec", "Rate of counts per second, 32bit");           
           pcg.addCounter("# of Filters/Sec", "Rate of counts per second, 32bit");           
           pcg.addCounter("# of Finallys/Sec", "Rate of counts per second, 32bit");           
           pcg.addCounter("Throw to Catch Depth/Sec", "Number of items, 32bit");
            allPerfCategory.Add(pcg);

            pcg = new perfCategory(".NET CLR Memory", ".net memory counter");
           pcg.addCounter("# Gen 0 Collections", "");           
           pcg.addCounter("# Gen 1 Collections", "");           
           pcg.addCounter("# Gen 2 Collections", "");           
           pcg.addCounter("Promoted Memory from Gen 0", "");           
           pcg.addCounter("Promoted Memory from Gen 1", "");           
           pcg.addCounter("Gen 0 Promoted Bytes/Sec", "");           
           pcg.addCounter("Gen 1 Promoted Bytes/Sec", "");           
           pcg.addCounter("Promoted Finalization-Memory from Gen 0", "");           
           pcg.addCounter("Gen 1 heap size", "");           
           pcg.addCounter("Gen 2 heap size", "");           
           pcg.addCounter("Large Object Heap size", "");           
           pcg.addCounter("Finalization Survivors", "");           
           pcg.addCounter("# GC Handles", "");           
           pcg.addCounter("Allocated Bytes/sec", "");           
           pcg.addCounter("# Induced GC", "");           
           pcg.addCounter("% Time in GC", "");           
           pcg.addCounter("# Bytes in all Heaps", "");           
           pcg.addCounter("# Total committed Bytes", "");           
           pcg.addCounter("# Total reserved Bytes", "");           
           pcg.addCounter("# of Pinned Objects", "");           
           pcg.addCounter("# of Sink Blocks in use", "");
            allPerfCategory.Add(pcg);


            pcg = new perfCategory(".NET CLR Remoting", ".net memory counter");
           pcg.addCounter("Remote Calls/sec", "");           
           pcg.addCounter("Total Remote Calls", "");           
           pcg.addCounter("Channels", "");           
           pcg.addCounter("Context Proxies", "");           
           pcg.addCounter("Context-Bound Classes Loaded", "");           
           pcg.addCounter("Context-Bound Objects Alloc / sec", "");           
           pcg.addCounter("Contexts", "");
            allPerfCategory.Add(pcg);



            pcg = new perfCategory(".NET CLR Loading", ".net memory counter");
           pcg.addCounter("Current Classes Loaded", "");           
           pcg.addCounter("Total Classes Loaded", "");           
           pcg.addCounter("Rate of Classes Loaded", "");           
           pcg.addCounter("Current appdomains", "");           
           pcg.addCounter("Total Appdomains", "");           
           pcg.addCounter("Rate of appdomains", "");           
           pcg.addCounter("Current Assemblies", "");           
           pcg.addCounter("Total Assemblies", "");           
           pcg.addCounter("Rate of Assemblies", "");           
           pcg.addCounter("Total # of Load Failures", "");           
           pcg.addCounter("Rate of Load Failures", "");           
           pcg.addCounter("Bytes in Loader Heap", "");           
           pcg.addCounter("Total appdomains unloaded", "");           
           pcg.addCounter("Rate of appdomains unloaded", "Rate of counts per second");
            allPerfCategory.Add(pcg);


            pcg = new perfCategory(".NET CLR Loading", ".net memory counter");
           pcg.addCounter("Total # of Contentions", "");           
           pcg.addCounter("Contention Rate / sec", "");
            pcg.addCounter("Current Queue Length", "");           
           pcg.addCounter("Queue Length Peak", "");           
           pcg.addCounter("Queue Length / sec", "");           
           pcg.addCounter("# of current logical Threads", "");           
           pcg.addCounter("# of current physical Threads", "");           
           pcg.addCounter("# of current recognized threads", "");           
           pcg.addCounter("# of total recognized threads", "rate of recognized threads / sec");
            allPerfCategory.Add(pcg);

            pcg = new perfCategory(".NET CLR Interop", ".net interop counter");
           pcg.addCounter("# of CCWs", "");           
           pcg.addCounter("# of Stubs", "");           
           pcg.addCounter("# of marshalling", "");
            allPerfCategory.Add(pcg);

            pcg = new perfCategory(".NET CLR Security", ".net Security counter");
           pcg.addCounter("# Link Time Checks", "");           
           pcg.addCounter("% Time in RT checks", "");           
           pcg.addCounter("Total Runtime Checks", "");           
           pcg.addCounter("Stack Walk Depth", "");
            allPerfCategory.Add(pcg);



            pcg = new perfCategory("Mono Threadpool", "This category is Mono-specific",true);
           pcg.addCounter("Work Items Added", "");           
           pcg.addCounter("Work Items Added/Sec", "");           
           pcg.addCounter("IO Work Items Added", "");           
           pcg.addCounter("IO Work Items Added/Sec", "");           
           pcg.addCounter("# of Threads", "");           
           pcg.addCounter("# of IO Threads", "");
            allPerfCategory.Add(pcg);



            pcg = new perfCategory("Network Interface", "");
           pcg.addCounter("Bytes Received/sec", "");           
           pcg.addCounter("Bytes Sent/sec", "");           
           pcg.addCounter("Bytes Total/sec", "");
            allPerfCategory.Add(pcg);



        }
        private static string GetProcessInstanceName(int pid)
        {
            //PerformanceCounterCategory cat = new PerformanceCounterCategory("Process");

            //string[] instances = cat.GetInstanceNames();
            //foreach (string instance in instances)
            //{

            //    using (PerformanceCounter cnt = new PerformanceCounter("Process",
            //         "ID Process", instance, true))
            //    {
            //        int val = (int)cnt.RawValue;
            //        if (val == pid)
            //        {
            //            return instance;
            //        }
            //    }
            //}
            throw new Exception("Could not find performance counter " +
                "instance name for current process. This is truly strange ...");
        }
        public   static object getCounterValue( pCounter pc)
        {
            throw new NotImplementedException();
            Process process = Process.GetCurrentProcess();
            var isUnix = Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;
          //  var instanceName = isUnix ? string.Format("{0}/{1}", process.Id, process.ProcessName) : process.ProcessName.TrimEnd(".vshost".ToArray());
            var instanceName = isUnix ? string.Format("{0}/{1}", process.Id, process.ProcessName) : GetProcessInstanceName(process.Id);
            Console.WriteLine("counter create");

          //var   counter = new PerformanceCounter(pc.ownCategory.Name, pc.Name, instanceName);
          //  Console.WriteLine("counter value:{0}", counter.RawValue);
            //return counter.RawValue;
          
        }
      public  static perfCategory getPerfCategoryByName(string name)
        {
            var y = (from x in allPerfCategory where x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase) select x).ToList();
            if (y.Count>0)
                return y.First();
            else
                return null;
        }

    }
}

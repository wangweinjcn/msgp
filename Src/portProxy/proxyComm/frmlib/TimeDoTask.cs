using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Globalization;
using log4net;
using System.Threading.Tasks;

namespace FrmLib.Extend
{
    


    public  enum enum_taskType {interval=0,everyday=1,everyHour=2,everyMinute=3,everySecond=4 };
    public class TimDoEventArgs : EventArgs
    {
        public readonly string memo;
        public TimDoEventArgs()
        {

        }
        public TimDoEventArgs(string memo)
        {
            this.memo = memo;
        }
    }
    /// <summary>
    /// 所有任务都应该集成该接口
    /// </summary>
    public interface ICronTask
    {

    }

    public delegate void TimeNowDoEventHandler();

  public   class TimeDoTask
    {
        private int _timeInterval = 10;
        private readonly Timer _timer;
        private TimeNowDoEventHandler _dodunc;
        private bool nowFuncDoing = false;
        private string _timeEvery;
        private enum_taskType tasktype = 0;
        private DateTime nextdotime;
        
        private void setNextDoTaskTime()
        {

            IFormatProvider culture = new CultureInfo("zh-CN", true);
            string nowdate = DateTime.Now.ToString("yyyyMMdd");
            DateTime dt = DateTime.ParseExact(nowdate + _timeEvery, "yyyyMMddHHmmss", null);
            if (System.DateTime.Compare(dt, System.DateTime.Now) > 0)
                nextdotime = dt;
            else
            { 
            switch ((int)tasktype)
            {
                case (int)enum_taskType.everyday:
                    nextdotime = dt.AddDays(1);
                    break;
                case (int)enum_taskType.everyHour:
                    nextdotime = dt.AddHours(1);
                    break;
                case (int)enum_taskType.everyMinute:
                    nextdotime = dt.AddMinutes(1);
                    break;
                case (int)enum_taskType.everySecond:
                    nextdotime = dt.AddSeconds(1);
                    break;
            }
            }
        }
        private bool taskShouldStart()
        {
            DateTime nowdt = DateTime.Now;
            if (nowdt.Year == nextdotime.Year && nowdt.Month == nextdotime.Month
                && nowdt.Day == nextdotime.Day && nowdt.Hour == nextdotime.Hour
                && nowdt.Minute == nextdotime.Minute && nowdt.Second == nextdotime.Second)
                return true;
            else
                return false;
        }
        private void pTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (tasktype == enum_taskType.interval)
            {
                if (!nowFuncDoing)
                {
                    try
                    {
                        lock (this)
                        {
                            nowFuncDoing = true;
                        }
                     //   FrmLib.Log.commLoger.runLoger.Info( string.Format("now do func {0} at:{1}", _dodunc.Method.ToString(), DateTime.Now.ToLongTimeString()));
                        //  Task.Factory.StartNew(delegate { _dodunc(); }) ;
                        _dodunc();
                       // FrmLib.Log.commLoger.runLoger.Info(string.Format("now do func {0} end:{1}", _dodunc.Method.ToString(), DateTime.Now.ToLongTimeString()));
                    }
                    catch (Exception exp)
                    {
                        string errofmsg=FrmLib.Extend.tools_static.getExceptionMessage(exp);
                        
                       FrmLib.Log.commLoger.runLoger.Error(string.Format("now do func {0} exception :{1},stack:{2}", _dodunc.Method.ToString(),
                            errofmsg + System.Environment.NewLine, exp.StackTrace));
                    }
                    finally
                    {
                        nowFuncDoing = false;
                    }
                }
            }
            else
            {
                if (taskShouldStart() && !nowFuncDoing)
                {

                    try
                    {
                        lock (this)
                        {
                            nowFuncDoing = true;
                        }
                        FrmLib.Log.commLoger.runLoger.Debug(string.Format("now do func {0} at:{1}", _dodunc.Method.ToString(), DateTime.Now.ToLongTimeString()));
                     Task.Factory.StartNew(delegate { _dodunc(); });
                        FrmLib.Log.commLoger.runLoger.Debug(string.Format("now do func {0} end at:{1}", _dodunc.Method.ToString(), DateTime.Now.ToLongTimeString()));
                    }
                    catch (Exception exp)
                    {

                        string errofmsg;
                        if (exp.InnerException != null)
                            errofmsg = exp.InnerException.Message;
                        else
                            errofmsg = exp.Message;
                        FrmLib.Log.commLoger.runLoger.Error(string.Format("now do func {0} exception :{1},stack:{2}", _dodunc.Method.ToString(), 
                            errofmsg+System.Environment.NewLine,exp.StackTrace));

                    }
                    finally
                    {
                        nowFuncDoing = false;
                        setNextDoTaskTime();
                    }
                   
                }
            
            
            }
            
        }
        public TimeDoTask(int timeInterval, TimeNowDoEventHandler func)
        {
            this.tasktype = enum_taskType.interval;
            this._timeInterval = timeInterval;
             _timer = new Timer(_timeInterval) { AutoReset = true };
            _timer.Elapsed += pTimer_Elapsed;
            _dodunc = func;
            
        }
      /// <summary>
      /// 
      /// </summary>
      /// <param name="timedo">需求做的时间，格式HHmmss</param>
      /// <param name="func"></param>
      /// <param name="tasktype"></param>
        public TimeDoTask(string timedo, TimeNowDoEventHandler func,enum_taskType tasktype)
        {
            this.tasktype = tasktype;
            _dodunc = func; 
            this._timeInterval = 10;
            _timer = new Timer(_timeInterval) { AutoReset = true };
            _timer.Elapsed += pTimer_Elapsed;
            _timeEvery=timedo;
            setNextDoTaskTime();
            
        }
        public void start()
        {
            _timer.Start();

        }
        public void stop()
        {
            _timer.Stop();
        }
    }
}

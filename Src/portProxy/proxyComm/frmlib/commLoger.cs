using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace FrmLib.Log
{
    public class myLogger
    {
        Assembly logrepositoryname { get { return Assembly.GetEntryAssembly(); } }
        private ILog loger;
        private string loggername = "";

        public myLogger(string name)
        {
            loggername = name;

        }
        #region Debug
        public void Debug(object message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
          
            loger = LogManager.GetLogger(logrepositoryname, loggername);
            var filename = System.IO.Path.GetFileName(sourceFilePath);
            string complexMessage = string.Format("[{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, message);
          

            loger.Debug(complexMessage);
         
        }
        public void Debug(object message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            loger = LogManager.GetLogger(logrepositoryname, loggername);
            var filename = System.IO.Path.GetFileName(sourceFilePath);
            string complexMessage = string.Format("[{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, message);
            loger.Debug(complexMessage,exception);
        }
        public void DebugFormat(IFormatProvider provider, string format, params object[] args)
        {
            throw new NotImplementedException();
        }
        public void DebugFormat(string format, params object[] args)
        {
            try
            {
                loger = LogManager.GetLogger(logrepositoryname, loggername);
                StackTrace stackTrace = new StackTrace();           // get call stack
                StackFrame[] stackFrames = stackTrace.GetFrames();
                if (stackFrames.Count() < 2)
                {
                    loger.DebugFormat(format, args);
                    return;
                }
                StackFrame callingFrame = stackFrames[1];
                var method = callingFrame.GetMethod();

                string memberName = method.Name;
                string filename = method.DeclaringType.Name;
                string sourceLineNumber = "?";
                string complexMessage = string.Format(" [{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, string.Format(format, args));
                loger.Debug(complexMessage);
            }
            catch (Exception e)
            {
                loger.Debug("输出日志错误", e);
            }
        }
        public void DebugFormat(string format, object arg0)
        {
            DebugFormat(format, new object[] { arg0 });
        }
        public void DebugFormat(string format, object arg0, object arg1, object arg2)
        {
            DebugFormat(format, new object[] { arg0,arg1,arg2 });
        }
        public void DebugFormat(string format, object arg0, object arg1)
        {
            DebugFormat(format, new object[] { arg0,arg1 });
        }
        #endregion
        #region Error
        public void Error(object message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            loger = LogManager.GetLogger(logrepositoryname, loggername);
            var filename = System.IO.Path.GetFileName(sourceFilePath);
            string complexMessage = string.Format("[{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, message);
            loger.Error(complexMessage);
        }
        public void Error(object message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            loger = LogManager.GetLogger(logrepositoryname, loggername);
            var filename = System.IO.Path.GetFileName(sourceFilePath);
            string complexMessage = string.Format("[{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, message);
            loger.Error(complexMessage, exception);
        }
        public void ErrorFormat(IFormatProvider provider, string format, params object[] args)
        {
            throw new NotImplementedException();
        }
        public void ErrorFormat(string format, params object[] args)
        {
            try
            {
                loger = LogManager.GetLogger(logrepositoryname, loggername);
                StackTrace stackTrace = new StackTrace();           // get call stack
                StackFrame[] stackFrames = stackTrace.GetFrames();
                if (stackFrames.Count() < 2)
                {
                    loger.ErrorFormat(format, args);
                    return;
                }
                StackFrame callingFrame = stackFrames[1];
                var method = callingFrame.GetMethod();

                string memberName = method.Name;
                string filename = method.DeclaringType.Name;
                string sourceLineNumber = "?";
                string complexMessage = string.Format(" [{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, string.Format(format, args));
                loger.Error(complexMessage);
            }
            catch (Exception e)
            {
                loger.Error("输出日志错误", e);
            }
        }
        public void ErrorFormat(string format, object arg0)
        {
            ErrorFormat(format, new object[] { arg0 });
        }
        public void ErrorFormat(string format, object arg0, object arg1, object arg2)
        {
            ErrorFormat(format, new object[] { arg0, arg1, arg2 });
        }
        public void ErrorFormat(string format, object arg0, object arg1)
        {
            ErrorFormat(format, new object[] { arg0, arg1 });
        }
        #endregion
        #region Fatal
        public void Fatal(object message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            loger = LogManager.GetLogger(logrepositoryname, loggername);
            var filename = System.IO.Path.GetFileName(sourceFilePath);
            string complexMessage = string.Format("[{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, message);
            loger.Fatal(complexMessage);
        }
        public void Fatal(object message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            loger = LogManager.GetLogger(logrepositoryname, loggername);
            var filename = System.IO.Path.GetFileName(sourceFilePath);
            string complexMessage = string.Format("[{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, message);
            loger.Fatal(complexMessage, exception);
        }
        public void FatalFormat(IFormatProvider provider, string format, params object[] args)
        {
            throw new NotImplementedException();
        }
        public void FatalFormat(string format, params object[] args)
        {
            try
            {
                loger = LogManager.GetLogger(logrepositoryname, loggername);
                StackTrace stackTrace = new StackTrace();           // get call stack
                StackFrame[] stackFrames = stackTrace.GetFrames();
                if (stackFrames.Count() < 2)
                {
                    loger.FatalFormat(format, args);
                    return;
                }
                StackFrame callingFrame = stackFrames[1];
                var method = callingFrame.GetMethod();

                string memberName = method.Name;
                string filename = method.DeclaringType.Name;
                string sourceLineNumber = "?";
                string complexMessage = string.Format(" [{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, string.Format(format, args));
                loger.Fatal(complexMessage);
            }
            catch (Exception e)
            {
                loger.Fatal("输出日志错误", e);
            }
        }
        public void FatalFormat(string format, object arg0)
        {
            FatalFormat(format, new object[] { arg0 });
        }
        public void FatalFormat(string format, object arg0, object arg1, object arg2)
        {
            FatalFormat(format, new object[] { arg0, arg1, arg2 });
        }
        public void FatalFormat(string format, object arg0, object arg1)
        {
            FatalFormat(format, new object[] { arg0, arg1 });
        }
        #endregion
        #region Info
        public void Info(object message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            loger = LogManager.GetLogger(logrepositoryname, loggername);
            var filename = System.IO.Path.GetFileName(sourceFilePath);
            string complexMessage = string.Format("[{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, message);
            loger.Info(complexMessage);
        }
        public void Info(object message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            loger = LogManager.GetLogger(logrepositoryname, loggername);
            var filename = System.IO.Path.GetFileName(sourceFilePath);
            string complexMessage = string.Format("[{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, message);
            loger.Info(complexMessage, exception);
        }
        public void InfoFormat(IFormatProvider provider, string format, params object[] args)
        {
            throw new NotImplementedException();
        }
        public void InfoFormat(string format, params object[] args)
        {
            try
            {
                loger = LogManager.GetLogger(logrepositoryname, loggername);
                StackTrace stackTrace = new StackTrace();           // get call stack
                StackFrame[] stackFrames = stackTrace.GetFrames();
                if (stackFrames.Count() < 2)
                {
                    loger.InfoFormat(format, args);
                    return;
                }
                StackFrame callingFrame = stackFrames[1];
                var method = callingFrame.GetMethod();

                string memberName = method.Name;
                string filename = method.DeclaringType.Name;
                string sourceLineNumber = "?";
                string complexMessage = string.Format(" [{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, string.Format(format, args));
                loger.Info(complexMessage);
            }
            catch (Exception e)
            {
                loger.Info("输出日志错误", e);
            }
        }
        public void InfoFormat(string format, object arg0)
        {
            InfoFormat(format, new object[] { arg0 });
        }
        public void InfoFormat(string format, object arg0, object arg1, object arg2)
        {
            InfoFormat(format, new object[] { arg0, arg1, arg2 });
        }
        public void InfoFormat(string format, object arg0, object arg1)
        {
            InfoFormat(format, new object[] { arg0, arg1 });
        }
        #endregion
        #region Warn
        public void Warn(object message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            loger = LogManager.GetLogger(logrepositoryname, loggername);
            var filename = System.IO.Path.GetFileName(sourceFilePath);
            string complexMessage = string.Format("[{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, message);
            loger.Warn(complexMessage);
        }
        public void Warn(object message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            loger = LogManager.GetLogger(logrepositoryname, loggername);
            var filename = System.IO.Path.GetFileName(sourceFilePath);
            string complexMessage = string.Format("[{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, message);
            loger.Warn(complexMessage, exception);
        }
        public void WarnFormat(IFormatProvider provider, string format, params object[] args)
        {
            throw new NotImplementedException();
        }
        public void WarnFormat(string format, params object[] args)
        {
            try
            {
                loger = LogManager.GetLogger(logrepositoryname, loggername);
                StackTrace stackTrace = new StackTrace();           // get call stack
                StackFrame[] stackFrames = stackTrace.GetFrames();
                if (stackFrames.Count() < 2)
                {
                    loger.WarnFormat(format, args);
                    return;
                }
                StackFrame callingFrame = stackFrames[1];
                var method = callingFrame.GetMethod();

                string memberName = method.Name;
                string filename = method.DeclaringType.Name;
                string sourceLineNumber = "?";
                string complexMessage = string.Format(" [{0}:{1}@{2}]{3}", memberName, sourceLineNumber, filename, string.Format(format, args));
                loger.Warn(complexMessage);
            }
            catch (Exception e)
            {
                loger.Warn("输出日志错误", e);
            }
        }
        public void WarnFormat(string format, object arg0)
        {
            WarnFormat(format, new object[] { arg0 });
        }
        public void WarnFormat(string format, object arg0, object arg1, object arg2)
        {
            WarnFormat(format, new object[] { arg0, arg1, arg2 });
        }
        public void WarnFormat(string format, object arg0, object arg1)
        {
            WarnFormat(format, new object[] { arg0, arg1 });
        }
        #endregion


    }
    public class commLoger
    {
        static Assembly logrepositoryname { get { return Assembly.GetEntryAssembly(); } }

        public static myLogger testloger = new myLogger("runInfo");
        /// <summary>
        /// 记录系统运行状况的日志,通常使用error和fatal级别
        /// </summary>
        public static myLogger runLoger = new myLogger("runInfo");
        

        // public static ILog testloger=LogManager.GetLogger(logrepositoryname,)
        /// <summary>
        /// 用于开发调试的日志，通常使用debug和info级别
        /// </summary>
        public static myLogger devLoger = new myLogger("devDebug");
        /// <summary>
        /// 用于数据分析使用日志，通常使用info级别
        /// </summary>
        public static myLogger biLoger = new myLogger("biLoger");
        /// <summary>
        /// 用于记录数据库相关日志，例如sql语句，通常使用info级别
        /// </summary>
        public static myLogger dbLoger = new myLogger("dbLoger");
        /// <summary>
        /// 用于记录性能调测相关日志，例如sql语句，通常使用debug级别
        /// </summary>
        public static myLogger perfLoger = new myLogger("perfLoger");

    }
}

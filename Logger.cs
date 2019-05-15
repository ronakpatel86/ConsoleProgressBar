/*
* @author  : Ronak Patel
* @version : 0.0.0.1
* @since   : 21 Oct 2014
* 
* Modification History :
* Date of Modification		Modified By			Changes made
* -------------------------------------------------------------------------------------------------------
* 
* 
*/

using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace ConsoleLogger
{
	public static class Logger
	{
		/// <summary>
		/// Gets the Date & Time in 'yyyy-MM-dd HH:mm:ss' format.
		/// </summary>
		private static string LogTime { get { return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); } }

		/// <summary>
		/// Writes the error log.
		/// </summary>
		/// <remarks>
		/// This is an Extension method for Exception object.
		/// </remarks>
		/// <param name="e"></param>
		/// <param name="methodInfo">
		/// Method Name
		/// </param>
		public static void WriteErrorLog(this Exception e, string methodInfo)
		{
			//bool result = false;

			string ErrorLogDirName = ConfigurationManager.AppSettings["ErrorLogDirectory"].ToString();
			string SingleLogFile = ConfigurationManager.AppSettings["SingleLogFile"].ToString();
			string LogFileHeader = ConfigurationManager.AppSettings["LogFileHeader"].ToString();
			bool IsSingleLogFile = false;

			string ErrorLogFileName = string.Empty;

			if (bool.TryParse(SingleLogFile, out IsSingleLogFile))
				IsSingleLogFile = Convert.ToBoolean(SingleLogFile);

			string ErrorLogDirPath = Directory.GetParent(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)).ToString() + "\\" + ErrorLogDirName;

			if (!Directory.Exists(ErrorLogDirPath))
				Directory.CreateDirectory(ErrorLogDirPath);

			string ErrorDate = DateTime.Now.ToShortDateString();
			string ErrorTime = DateTime.Now.ToLongTimeString();

			StreamWriter sw = null;

			try
			{
				if (IsSingleLogFile)
				{
					string LogFileName = ConfigurationManager.AppSettings["ErrorLogFileName"].ToString();
					string ErrorLogFile = ErrorLogDirPath + "\\" + LogFileName;

					if (!File.Exists(ErrorLogFile))
					{
						sw = new StreamWriter(ErrorLogFile);
						sw.WriteLine("---------------------------------------------------------------");
						sw.WriteLine(LogFileHeader + ": Error Details");
						sw.WriteLine("---------------------------------------------------------------");
						sw.WriteLine();
						sw.Flush();
						sw.Close();
					}

					sw = new StreamWriter(ErrorLogFile, true);
				}
				else
				{
					string LogFileName = "Error-" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".log";
					string ErrorLogFile = ErrorLogDirPath + "\\" + LogFileName;

					sw = new StreamWriter(ErrorLogFile);
					sw.WriteLine("---------------------------------------------------------------");
					sw.WriteLine(LogFileHeader + ": Error Details");
					sw.WriteLine("---------------------------------------------------------------");
					sw.WriteLine();
				}

				sw.WriteLine("Method		: " + methodInfo);
				sw.WriteLine("Date		: " + ErrorDate);
				sw.WriteLine("Time		: " + ErrorTime);
				sw.WriteLine("Error		: " + e.Message.ToString().Trim());
				sw.WriteLine("Stack Trace	: " + e.StackTrace.ToString().Trim());
				sw.WriteLine("_________________________________________________________________________");
				sw.WriteLine();
				sw.Flush();
				sw.Close();

				//result = true;
			}
			catch (Exception)
			{
				//result = false;
			}

			//return result;
		}

		/// <summary>
		/// Sends Errorlog file to configured email addresses.
		/// </summary>
		public static void SendLog()
		{
			try
			{
				string ErrorLogDirName = ConfigurationManager.AppSettings["ErrorLogDirectory"].ToString();
				string ApplicationLogDirName = ConfigurationManager.AppSettings["ApplicationLogDirectory"].ToString();
				string SendLogFlag = ConfigurationManager.AppSettings["SendLog"].ToString();
				bool SendLog = false;

				string ErrorLogFileName = string.Empty;

				if (bool.TryParse(SendLogFlag, out SendLog))
					SendLog = Convert.ToBoolean(SendLogFlag);

				string ApplicationPath = Directory.GetParent(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)).ToString();
				string ErrorLogFilePath = ApplicationPath + "\\" + ErrorLogDirName + "\\" + "Error.log";
				string ApplicationLogFilePath = ApplicationPath + "\\" + ApplicationLogDirName + "\\" + "Log-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log";

				if (SendLog && File.Exists(ErrorLogFilePath) && File.Exists(ApplicationLogFilePath))
				{
					DateTime lastUpdated = File.GetLastWriteTime(ErrorLogFilePath);

					if (lastUpdated.ToShortDateString() != DateTime.Now.ToShortDateString())
						SendLog = false;
				}

				if (SendLog)
				{
					string SMTPUser = ConfigurationManager.AppSettings["SMTPUser"].ToString();
					string SMTPPass = ConfigurationManager.AppSettings["SMTPPass"].ToString();

					SmtpClient smtp = new SmtpClient(ConfigurationManager.AppSettings["SMTPServer"].ToString());
					string smtpport = ConfigurationManager.AppSettings["SMTPPort"].ToString().Trim();

					int port = 0;
					if (Int32.TryParse(smtpport, out port))
						smtp.Port = port;

					smtp.EnableSsl = false;
					smtp.Timeout = 100000;
					smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

					if (!string.IsNullOrEmpty(SMTPUser) && !string.IsNullOrEmpty(SMTPPass))
						smtp.Credentials = new System.Net.NetworkCredential(SMTPUser, SMTPPass);
					else
						smtp.Credentials = CredentialCache.DefaultNetworkCredentials;

					string from = ConfigurationManager.AppSettings["From"].ToString();
					string fromDisplayName = ConfigurationManager.AppSettings["FromDisplayName"].ToString();
					string to = ConfigurationManager.AppSettings["SendErrorLogTo"].ToString();

					string subject = ConfigurationManager.AppSettings["ErrorLogEmailSubject"].ToString();
					string messageBody = ConfigurationManager.AppSettings["ErrorLogEmailBody"].ToString();

					MailMessage email = new MailMessage();
					email.From = new MailAddress(from, fromDisplayName);
					email.To.Add(to);
					email.Subject = subject;
					email.Body = messageBody;

					Attachment applicationLog = new Attachment(ApplicationLogFilePath, MediaTypeNames.Application.Octet);
					Attachment errorLog = new Attachment(ErrorLogFilePath, MediaTypeNames.Application.Octet);
					email.Attachments.Add(applicationLog);
					email.Attachments.Add(errorLog);

					smtp.Send(email);
				}
			}
			catch (Exception)
			{

			}
		}

		/// <summary>
		/// Writes the current line terminator.
		/// </summary>
		public static void WriteLine()
		{
			WriteLine(null, false);
		}

		/// <summary>
		/// Writes the log message in new line.
		/// </summary>
		/// <param name="log">
		/// A string log.
		/// </param>
		public static void WriteLine(string log)
		{
			WriteLog(log, LogVisibility.Visible, true, true);
		}

		/// <summary>
		/// Writes the log message in new line.
		/// </summary>
		/// <param name="log">
		/// A string log.
		/// </param>
		/// <param name="showLogTime">
		/// A System.Boolean value that specifies whether to record the Time Stamp of Log on not.
		/// </param>
		public static void WriteLine(string log, bool showLogTime)
		{
			WriteLog(log, LogVisibility.Visible, true, showLogTime);
		}

		/// <summary>
		/// Writes the specified string log, followed by the current line terminator.
		/// </summary>
		/// <param name="log">
		/// A string log.
		/// </param>
		/// <param name="visibility">
		/// A ConsoleLogger.LogVisibility value that specifies the log visibility in Console.
		/// </param>
		/// <param name="showLogTime">
		/// A System.Boolean value that specifies whether to record the Time Stamp of Log on not.
		/// </param>
		public static void WriteLine(string log, LogVisibility visibility, bool showLogTime)
		{
			WriteLog(log, visibility, true, showLogTime);
		}

		/// <summary>
		/// Writes the current line terminator.
		/// </summary>
		/// <param name="visibility">
		/// A ConsoleLogger.LogVisibility value that specifies the log visibility in Console.
		/// </param>
		/// <param name="showLogTime">
		/// A System.Boolean value that specifies whether to record the Time Stamp of Log on not.
		/// </param>
		public static void WriteLine(LogVisibility visibility, bool showLogTime)
		{
			WriteLine(null, visibility, showLogTime);
		}

		/// <summary>
		/// Writes the specified string log.
		/// </summary>
		/// <param name="log">
		/// A string log.
		/// </param>
		public static void Write(string log)
		{
			WriteLog(log, LogVisibility.Visible, false, true);
		}

		/// <summary>
		/// Writes the specified string log.
		/// </summary>
		/// <param name="log">
		/// A string log.
		/// </param>
		/// <param name="showLogTime">
		/// A System.Boolean value that specifies whether to record the Time Stamp of Log on not.
		/// </param>
		public static void Write(string log, bool showLogTime)
		{
			WriteLog(log, LogVisibility.Visible, false, showLogTime);
		}

		/// <summary>
		/// Writes the specified string log.
		/// </summary>
		/// <param name="log">
		/// A string log.
		/// </param>
		/// <param name="visibility">
		/// A ConsoleLogger.LogVisibility value that specifies the log visibility in Console.
		/// </param>
		public static void Write(string log, LogVisibility visibility, bool showLogTime)
		{
			WriteLog(log, visibility, false, showLogTime);
		}

		/// <summary>
		/// Writes the string log to the output stream and the application log file.
		/// <para>This is a private method.</para>
		/// </summary>
		/// <param name="log">
		/// A string log.
		/// </param>
		/// <param name="visibility">
		/// A ConsoleLogger.LogVisibility value that specifies the log visibility in Console.
		/// </param>
		/// <param name="isNewLine">
		/// A Boolean value that specifies whether to add the current line terminator after log or not.
		/// </param>
		private static void WriteLog(string log, LogVisibility visibility, bool isNewLine, bool showLogTime)
		{
			//bool result = false;

			string AppLogDirName = "ApplicationLog";

			if (ConfigurationManager.AppSettings["ApplicationLogDirectory"] != null)
				AppLogDirName = ConfigurationManager.AppSettings["ApplicationLogDirectory"].ToString();

			string AppLogDirPath = Directory.GetParent(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)).ToString() + "\\" + AppLogDirName;

			if (!Directory.Exists(AppLogDirPath))
				Directory.CreateDirectory(AppLogDirPath);

			//string LogFileName = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".log";
			string LogFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".log";
			string AppLogFile = AppLogDirPath + "\\" + LogFileName;

			StreamWriter sw = null;

			try
			{
				if (File.Exists(AppLogFile))
					sw = new StreamWriter(AppLogFile, true);
				else
					sw = new StreamWriter(AppLogFile);

				if (log != null)
				{
					if (log.ToLower().Contains("success!"))
					{
						Console.BackgroundColor = ConsoleColor.Green;
						Console.ForegroundColor = ConsoleColor.Black;
					}
					else if (log.ToLower().Contains("failed!"))
					{
						Console.BackgroundColor = ConsoleColor.Red;
						Console.ForegroundColor = ConsoleColor.Black;
					}
					else
					{
						Console.BackgroundColor = ConsoleColor.Black;
						Console.ForegroundColor = ConsoleColor.White;
					}

					if (isNewLine)
					{
						if (visibility == LogVisibility.Visible)
							Console.WriteLine(log);

						if (showLogTime)
							sw.WriteLine("[" + LogTime + "] : " + log);
						else
							sw.WriteLine(log);
					}
					else
					{
						Console.Write(log);

						if (showLogTime)
							sw.Write("[" + LogTime + "] : " + log);
						else
							sw.Write(log);
					}
				}
				else
				{
					if (visibility == LogVisibility.Visible)
						Console.WriteLine();

					sw.WriteLine();
				}

				sw.Flush();
				sw.Close();

				//result = true;
			}
			catch (Exception)
			{
				//result = false;
			}

			//return result;
		}
	}

	/// <summary>
	/// Log visibilty options.
	/// </summary>
	public enum LogVisibility
	{
		Visible, Hidden
	}
}
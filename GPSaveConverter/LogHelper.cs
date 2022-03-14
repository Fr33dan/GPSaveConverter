using NLog.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSaveConverter
{
    internal static class LogHelper
    {
        static LogHelper()
        {
            // Manually load configuration item factory so that extension is loaded.
            NLog.Config.ConfigurationItemFactory.Default = new ConfigurationItemFactory(typeof(NLog.LogFactory).Assembly,typeof(NLog.Windows.Forms.ToolStripItemTarget).Assembly);
            System.Xml.XmlReader xmlReader = System.Xml.XmlReader.Create(new System.IO.MemoryStream(GPSaveConverter.Properties.Resources.nlog_config));
            NLog.LogManager.Configuration = new XmlLoggingConfiguration(xmlReader, null);
        }
        public static NLog.Logger getClassLogger()
        {
            System.Diagnostics.StackFrame frame = new System.Diagnostics.StackFrame(1, false);

            return NLog.LogManager.GetLogger(frame.GetMethod().DeclaringType.FullName);

        }
    }
}

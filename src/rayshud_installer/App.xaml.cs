using log4net;
using log4net.Config;
using System.IO;
using System.Reflection;
using System.Windows;

namespace rayshud_installer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(App));

        protected override void OnStartup(StartupEventArgs e)
        {
            var repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(repository, new FileInfo("log4net.config"));
            logger.Info("        ======  Started Logging  ======        ");
            base.OnStartup(e);
        }
    }
}
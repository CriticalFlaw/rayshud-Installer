using log4net;
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
            log4net.Config.XmlConfigurator.Configure();
            logger.Info("        ======  Started Logging  ======        ");
            base.OnStartup(e);
        }
    }
}
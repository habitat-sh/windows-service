using System.ServiceProcess;

namespace HabService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new HabService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}

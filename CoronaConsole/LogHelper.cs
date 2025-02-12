using CoronaDVH.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoronaConsole
{
    public class LogHelper
    {
        internal static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                        .WriteTo.Console()
                        .CreateLogger();

            //Setup Micosoft Extensions Logging
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddSerilog())
                .BuildServiceProvider();

            ServiceLocator.ServiceProvider = serviceProvider;
        }
    }
}

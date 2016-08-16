using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using AllJoyn.Dsb;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace HeadlessAdapterApp
{
    public sealed class StartupTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {

            deferral = taskInstance.GetDeferral();

            try
            {
                await AllJoynDsbServiceManager.Current.StartAsync(new AllJoyn.EcobeeDSB.EcobeeDsbAdapter());
            }
            catch (Exception ex)
            {
                deferral.Complete();
                throw;
            }
        }
        
        private BackgroundTaskDeferral deferral;
    }
}

using AllJoyn.Dsb;
using BridgeRT;
using Ecobee;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace AllJoyn.EcobeeDSB
{
    public class EcobeeDsbAdapter : AllJoyn.Dsb.Adapter
    {
        private Dictionary<string, EcobeeDevice> ecobeeDevices = new Dictionary<string, EcobeeDevice>();
        private EcobeeClient client;
        private AdapterInterface iface;

        public EcobeeDsbAdapter() : base(new BridgeConfiguration(GetDeviceID(), "com.dotMorten.EcobeeDSB")
        {
            ModelName = "Ecobee DSB",
            DeviceName = "Ecobee DSB",
            Vendor = "Morten Nielsen"
        })
        {
            CreateInterfaces();
            if (AppKey != null)
            {
                CreateEcobeeClient();
            }
        }

        public string AppKey
        {
            get
            {
                if (ApplicationData.Current.LocalSettings.Values.ContainsKey("ApiKey"))
                    return ApplicationData.Current.LocalSettings.Values["ApiKey"] as string;
                return null;
            }
            set {
                //If APIKey changes, restart
                if (AppKey != value)
                {
                    if (tcs != null)
                        tcs.Cancel();
                    tcs = null;
                    client = null;
                    foreach(var item in ecobeeDevices)
                    {
                        base.RemoveDevice(item.Value);
                    }
                    ecobeeDevices.Clear();
                    ClearToken();
                    ApplicationData.Current.LocalSettings.Values["ApiKey"] = value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        CreateEcobeeClient();
                    }
                }
            }
        }

        private void CreateInterfaces()
        {
            AdapterBusObject abo = new AdapterBusObject("Configuration");
            iface = new AdapterInterface("org.dotMorten.Ecobee");
            iface.Properties.Add(new AdapterAttribute("Version", (ushort)1) { COVBehavior = SignalBehavior.Never });
            iface.Properties[0].Annotations.Add("org.alljoyn.Bus.DocString.En", "The interface version");

            bool isApiKeySet = ApplicationData.Current.LocalSettings.Values.ContainsKey("ApiKey") && !string.IsNullOrEmpty(ApplicationData.Current.LocalSettings.Values["ApiKey"] as string);
            var apiKeySetProp = new AdapterAttribute("IsApiKeySet", isApiKeySet) { COVBehavior = SignalBehavior.Always };
            apiKeySetProp.Annotations.Add("org.alljoyn.Bus.DocString.En", "Returns true if the Ecobee Developer API Key is set");
            iface.Properties.Add(apiKeySetProp);

            iface.Properties.Add(new AdapterAttribute("ApiKey", "", (o) =>
            {
                AppKey = o as string;
                apiKeySetProp.Value.Data = !string.IsNullOrEmpty(AppKey);
                //TODO: Raise prop changed for IsApiKeySet
                return AllJoynStatusCode.Ok;
            }, false));
            iface.Properties[2].Annotations.Add("org.alljoyn.Bus.DocString.En", "Sets the Ecobee Developer API Key");
            iface.Properties.Add(new AdapterAttribute("IsRegistered", isApiKeySet && GetToken() != null));
            iface.Properties[3].Annotations.Add("org.alljoyn.Bus.DocString.En", "Gets a value indicating whether the bridge has been registered with Ecobee");

            iface.Methods.Add(new AdapterMethod("RegisterApplication", "Gets an application authorization code. Visit https://www.ecobee.com/consumerportal/index.html#/my-apps/add/new to register this app", (a, b, c) =>
            {
                bool hasKey = ApplicationData.Current.LocalSettings.Values.ContainsKey("ApiKey") && !string.IsNullOrEmpty(ApplicationData.Current.LocalSettings.Values["ApiKey"] as string);
                if (!hasKey)
                    throw new ArgumentException("KeyNotSet");
                var getPinTask = client.BeginPinRequest(AppScope.SmartWrite);
                getPinTask.Wait();
                c["AuthorizationCode"] = getPinTask.Result.ecobeePin;
                c["ExpiresIn"] = getPinTask.Result.expires_in;
                BeginWaitForPin(getPinTask.Result);

            }, null, new IAdapterValue[] { new AdapterValue("AuthorizationCode", ""), new AdapterValue("ExpiresIn", 0) }));
            abo.Interfaces.Add(iface);
            BusObjects.Add(abo);
        }

        private async void BeginWaitForPin(PinRequestResult result)
        {
            try
            {
                var tokenResult = await client.BeginWaitForPin(result);
                SaveToken(tokenResult.refresh_token);
                Initialize();
            }
            catch
            {

            }
        }

        private static Guid GetDeviceID()
        {
            if (!Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("DSBDeviceId"))
            {
                Guid deviceId = Guid.NewGuid();
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["DSBDeviceId"] = deviceId;
                return deviceId;
            }
            return (Guid)Windows.Storage.ApplicationData.Current.LocalSettings.Values["DSBDeviceId"];
        }

        private string GetToken()
        {
            if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("RefreshToken"))
                return (string)Windows.Storage.ApplicationData.Current.LocalSettings.Values["RefreshToken"];
            return null;
        }
        private void SaveToken(string token)
        {
            Windows.Storage.ApplicationData.Current.LocalSettings.Values["RefreshToken"] = token;
            iface.Properties.Where(p => p.Value.Name == "IsRegistered").First().Value.Data = true;
        }
        private void ClearToken()
        {
            if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey("RefreshToken"))
                Windows.Storage.ApplicationData.Current.LocalSettings.Values.Remove("RefreshToken");
            iface.Properties.Where(p => p.Value.Name == "IsRegistered").First().Value.Data = false;
        }

        private void CreateEcobeeClient()
        {
            string token = GetToken();
            if (token != null)
            {
                client = new EcobeeClient(AppKey, token);
                Initialize();
            }
            else
            {
                client = new EcobeeClient(AppKey);
            }

            client.RefreshTokenUpdated += (s, e) =>
            {
                SaveToken(e);
            };
        }

        private async void Initialize()
        {
            tcs = new CancellationTokenSource();
            var token = tcs.Token;
            GetThermostatsResult t;
            try
            {
                t = await client.GetThermostatsAsync(new Selection()
                {
                    includeDevice = true,
                    includeSensors = true,
                    //// includeAlerts = true,
                    // includeElectricity = true,
                    //// includeEquipmentStatus = true,
                    includeEvents = true,
                    // includeExtendedRuntime = true,
                    // includeLocation = true,
                    // includeNotificationSettings = true, 
                    // includeOemConfig = true,
                    // includePrivacy = true,
                    // includeProgram = true,
                    // includeRuntime = true,
                    // includeSecuritySettings = true,
                    includeSettings = true,
                    // includeTechnician = true,
                    // includeUtility = true, 
                    // includeVersion = true,
                    // includeWeather = true
                });
            }
            catch (EcobeeRequestException err)
            {
                if (err.Message == "invalid_grant")
                {
                    ClearToken();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to initialize: {err.Message}. Retrying in 30 seconds...");
                    //Wait and retry
                    await Task.Delay(30000);
                    Initialize();
                }
                return;
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize: {ex.Message}. Retrying in 30 seconds...");
                //Wait and retry
                await Task.Delay(30000);
                if(!token.IsCancellationRequested)
                    Initialize();
                return;
            }
            List<IAdapterDevice> found = new List<IAdapterDevice>();
            foreach (var item in t.thermostatList)
            {
                EcobeeThermostatDevice thermostat = new EcobeeThermostatDevice(client, item);
                ecobeeDevices[thermostat.SensorId] = thermostat;
                thermostat.UpdateThermostat(item);
                AllJoynDsbServiceManager.Current.AddDevice(thermostat);
                //found.Add(d);
                foreach (var device in item.devices)
                {
                    if (device.deviceId == 0) continue; //Main sensor part of thermostat device
                    string deviceName = "ecobee " + device.name + "-" + device.deviceId;
                    var n = device.sensors.Select(s => s.name).Distinct();
                    if (n.Count() == 1)
                        deviceName = n.First();
                    var d = new EcobeeDevice(deviceName, device,
                        item.brand, item.modelNumber, item.thermostatRev, "eb" + item.identifier + device.name + device.deviceId,
                        item.name + " - " + device.name);
                    ecobeeDevices[d.SensorId] = d;
                    AllJoynDsbServiceManager.Current.AddDevice(d);
                    //found.Add(d);
                }
                foreach (var sensor in item.remoteSensors)
                {
                    if (ecobeeDevices.ContainsKey(sensor.id))
                        ecobeeDevices[sensor.id].UpdateReadings(sensor, false);
                }
            }
            StartUpdateLoop(token);
        }

        CancellationTokenSource tcs;
        private async void StartUpdateLoop(CancellationToken cancelToken)
        {
            GetThermostatsResult t = null;
            while (!cancelToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                if (cancelToken.IsCancellationRequested)
                    return;
                try
                {
                    t = await client.GetThermostatsAsync(new Selection()
                    {
                        includeSensors = true,
                        includeSettings = true
                    });
                }
                catch
                {
                    continue;
                }
                foreach (var item in t.thermostatList)
                {
                    var thermostat = ecobeeDevices.Values.OfType<EcobeeThermostatDevice>().Where(th => th.SerialNumber == "eb" + item.identifier).FirstOrDefault();
                    thermostat?.UpdateThermostat(item);
                    foreach (var sensor in item.remoteSensors)
                    {
                        if (ecobeeDevices.ContainsKey(sensor.id))
                            ecobeeDevices[sensor.id].UpdateReadings(sensor);
                    }
                }
            }
        }

    }
}

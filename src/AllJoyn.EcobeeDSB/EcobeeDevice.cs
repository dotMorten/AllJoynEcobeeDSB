using AllJoyn.Dsb;
using BridgeRT;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllJoyn.EcobeeDSB
{
    internal class EcobeeDevice : AdapterDevice
    {
        public Ecobee.Device _sensor;
        Dictionary<string, IAdapterInterface> interfaces = new Dictionary<string, IAdapterInterface>();
        public EcobeeDevice(string name, Ecobee.Device sensor, string brand, string modelNumber, string thermostatRev, string identifier, string description) 
            : base(name, brand, modelNumber, thermostatRev, identifier, description )
        {
            _sensor = sensor;
            Icon = new AdapterIcon(new Uri("ms-appx:///AllJoyn.EcobeeDSB/Icons/sensor.png"));
            BusObjects.Add(new AdapterBusObject("Environment"));
            foreach (var item in sensor.sensors)
            {
                IAdapterInterface iface = null;
                if (item.type == "temperature")
                {
                    iface = CreateTemperatureInterface(0d);
                }
                else if (item.type == "humidity")
                {
                    iface = CreateHumidityInterface(0d);
                }
                else if (item.type == "occupancy")
                {
                    iface = CreateOccupancyInterface();
                }
                if (iface != null)
                {
                    base.BusObjects[0].Interfaces.Add(iface);
                    interfaces[item.type] = iface;
                }
            }
            base.CreateEmitSignalChangedSignal();
        }
        private static AdapterInterface CreateTemperatureInterface(double currentValue)
        {
            AdapterInterface iface = new AdapterInterface("org.alljoyn.SmartSpaces.Environment.CurrentTemperature");
            iface.Properties.Add(new AdapterAttribute("Version", (ushort)1) { COVBehavior = SignalBehavior.Never });
            iface.Properties.Add(new AdapterAttribute("CurrentValue", currentValue) { COVBehavior = SignalBehavior.Always });
            iface.Properties[1].Annotations.Add("org.alljoyn.Bus.Type.Units", "degrees Celcius");
            iface.Properties.Add(new AdapterAttribute("Precision", 0.1d) { COVBehavior = SignalBehavior.Always });
            iface.Properties.Add(new AdapterAttribute("UpdateMinTime", (ushort)3000) { COVBehavior = SignalBehavior.Always });
            return iface;
        }

        private static AdapterInterface CreateHumidityInterface(double currentValue)
        {
            var iface = new AdapterInterface("org.alljoyn.SmartSpaces.Environment.CurrentHumidity");
            iface.Properties.Add(new AdapterAttribute("Version", (ushort)1) { COVBehavior = SignalBehavior.Never });
            iface.Properties[0].Annotations.Add("org.alljoyn.Bus.DocString.En", "The interface version");
            iface.Properties.Add(new AdapterAttribute("CurrentValue", currentValue) { COVBehavior = SignalBehavior.Always });
            iface.Properties[1].Annotations.Add("org.alljoyn.Bus.DocString.En", "Current relative humidity value");
            iface.Properties[1].Annotations.Add("org.alljoyn.Bus.Type.Min", "0");
            iface.Properties.Add(new AdapterAttribute("MaxValue", 100d) { COVBehavior = SignalBehavior.Always });
            iface.Properties[2].Annotations.Add("org.alljoyn.Bus.DocString.En", "Maximum value allowed for represented relative humidity");
            return iface;
        }

        private AdapterInterface CreateOccupancyInterface()
        {
            var iface = new AdapterInterface("org.dotMorten.SmartSpaces.Environment.Occupancy");
            iface.Properties.Add(new AdapterAttribute("Version", (ushort)1) { COVBehavior = SignalBehavior.Never });
            iface.Properties.Add(new AdapterAttribute("CurrentValue", false) { COVBehavior = SignalBehavior.Always });
            return iface;
        }

        public void UpdateReadings(Ecobee.RemoteSensor sensorReading, bool raiseSignalChanged = true)
        {
            foreach(var item in sensorReading.capability)
            {
                if(interfaces.ContainsKey(item.type))
                {
                    var iface = interfaces[item.type] as AdapterInterface;
                    var attr = iface.Properties.Where(a => a.Value.Name == "CurrentValue").First();
                    var t = attr.Value.Data.GetType();
                    var v = Convert.ChangeType(item.value, t, System.Globalization.CultureInfo.InvariantCulture);
                    if(item.type == "temperature")
                    {
                        var f = (double)v;
                        f = f * .1;
                        f = (f - 32) * (5d / 9d);
                        f = Math.Round(f, 1);
                        v = f;
                    }
                    if (!object.Equals(attr.Value.Data, v))
                    {
                        attr.Value.Data = v;
                        if(raiseSignalChanged)
                            SignalChangeOfAttributeValue(iface, attr);
                    }
                }
            }
        }

        public virtual string SensorId
        {
            get
            {
                return $"{_sensor.name}:{_sensor.deviceId}";
            }
        }
    }
}

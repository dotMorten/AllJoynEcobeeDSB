using AllJoyn.Dsb;
using BridgeRT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllJoyn.EcobeeDSB
{
    internal class EcobeeThermostatDevice : EcobeeDevice
    {
        private Ecobee.Thermostat _thermostat;
        private Ecobee.EcobeeClient _client;
        private AdapterInterface _hvacMode;
        private Dictionary<string, IAdapterProperty> propertyList = new Dictionary<string, IAdapterProperty>();

        public EcobeeThermostatDevice(Ecobee.EcobeeClient client, Ecobee.Thermostat thermostat)
            : base(thermostat.name, thermostat.devices.Where(d => d.deviceId == 0).FirstOrDefault(),
                  thermostat.brand, thermostat.modelNumber, thermostat.thermostatRev, "eb" + thermostat.identifier, "")
        {
            _client = client;
            _thermostat = thermostat;
            Icon = new AdapterIcon(new Uri("ms-appx:///AllJoyn.EcobeeDSB/Icons/ecobee3.png"));
            AdapterBusObject abo = new AdapterBusObject("Operation");
            abo.Interfaces.Add(CreateHvacFanMode(0));
            BusObjects.Add(abo);
            UpdateThermostat(_thermostat, false);
            // TODO:
            // org.alljoyn.SmartSpaces.Environment.TargetTemperature x 2 (Environment/Heat and Environment/Cool)
            // org.alljoyn.SmartSpaces.Operation.ClimateControlMode
            // org.alljoyn.SmartSpaces.Operation.FilterStatus
            // org.alljoyn.SmartSpaces.Operation.HeatingZone
            // org.alljoyn.SmartSpaces.Operation.ResourceSaving
        }

        private static AdapterInterface CreateHvacFanMode(ushort currentMode)
        {
            AdapterInterface iface = new AdapterInterface("org.alljoyn.SmartSpaces.Operation.HvacFanMode");
            // iface.Annotations.Add("org.alljoyn.Bus.Enum.Mode.Value.Auto", "0");
            // iface.Annotations.Add("org.alljoyn.Bus.Enum.Mode.Value.Circulation", "1");
            // iface.Annotations.Add("org.alljoyn.Bus.Enum.Mode.Value.Continuous", "2");
            iface.Properties.Add(new AdapterAttribute("Version", (ushort)1) { COVBehavior = SignalBehavior.Never });
            iface.Properties.Add(new AdapterAttribute("Mode", currentMode, OnHvacFanModeSet) { COVBehavior = SignalBehavior.Always });
            iface.Properties[1].Annotations.Add("org.alljoyn.Bus.Type.DocString.En", "Current mode of device.");
            iface.Properties[1].Annotations.Add("org.alljoyn.Bus.Type.Name", "[Mode]");
            iface.Properties.Add(new AdapterAttribute("SupportedModes", new ushort[] { 0, 1, 2 }) { COVBehavior = SignalBehavior.Always });
            return iface;
        }

        private static AllJoynStatusCode OnHvacFanModeSet(object arg)
        {
            return AllJoynStatusCode.Ok;
            // throw new NotImplementedException();
        }


        public void UpdateThermostat(Ecobee.Thermostat thermostat, bool raiseSignalChanged = true)
        {
            if (thermostat.Settings != null)
            {
                ushort mode = 0;
                switch(thermostat.Settings.hvacMode)
                {
                    case "off":
                    case "auxHeatOnly":
                    case "cool":
                    case "heat":
                    case "auto":
                    default:
                        mode = 0;
                        break;
                }
                var property = _hvacMode.Properties.Where(p => p.Value.Name == "Mode").First();
                if (property.Value.Data != (object)mode)
                {
                    property.Value.Data = mode;
                    SignalChangeOfAttributeValue(_hvacMode, property);
                }
            }
        }
    }
}

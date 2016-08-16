using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Ecobee
{
    public partial class EcobeeClient
    {
        public Task<GetThermostatsResult> GetThermostatsAsync(Selection selection = null, int page = 0)
        {
            string url = $"{ApiEndpoint}thermostat";
            GetThermostatsParameter p = new GetThermostatsParameter()
            {
                selection = selection ?? new Selection(),
                page = page
            };
            return GetDataAsync<GetThermostatsResult>(url, p);
        }


        [DataContract]
        private class GetThermostatsParameter
        {
            [DataMember]
            public Selection selection { get; set; }
            [DataMember(EmitDefaultValue = false)]
            public int page { get; set; }
        }
    }

    [DataContract]
    public class Selection
    {
        public SelectionType SelectionType
        {
            get
            {
                return (SelectionType)Enum.Parse(typeof(SelectionType), selectionType);
            }
            set
            {
                selectionType = value.ToString();
            }
        }


        [DataMember(IsRequired = true)]
        private string selectionType { get; set; } = "registered";

        [DataMember(IsRequired = true)]
        public string selectionMatch { get; set; } = "";

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeRuntime { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeExtendedRuntime { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeElectricity { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeSettings { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeLocation { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeProgram { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeEvents { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeDevice { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeTechnician { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeUtility { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeAlerts { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeWeather { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeOemConfig { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeEquipmentStatus { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeNotificationSettings { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includePrivacy { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeVersion { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeSecuritySettings { get; set; }
        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public bool includeSensors { get; set; }
    }

    public enum SelectionType
    {
        registered, thermostats, managementSet
    }

    [DataContract]
    public class Page
    {
        [DataMember] public int page { get; set; }
        [DataMember] public int totalPages { get; set; }
        [DataMember] public int pageSize { get; set; }
        [DataMember] public int total { get; set; }
    }

    [DataContract]
    public class GetThermostatsResult
    {
        [DataMember] public Page page { get; set; }
        [DataMember] public Thermostat[] thermostatList { get; set; }
        [DataMember] public Status status { get; set; }
    }

    [DataContract]
    public class Thermostat
    {
        [DataMember] public string identifier { get; set; }
        [DataMember] public string name { get; set; }
        [DataMember] public string brand { get; set; }   
        [DataMember] public string modelNumber { get; set; }
        [DataMember] public string thermostatRev { get; set; }
        [DataMember] public Device[] devices { get; set; }
        [DataMember] public RemoteSensor[] remoteSensors { get; set; }
        [DataMember]
        public Settings Settings { get; set; }
    }

    [DataContract]
    public class Device
    {
        [DataMember] public int deviceId { get; set; }
        [DataMember] public string name { get; set; }
        [DataMember] public Sensor[] sensors { get; set; }
    }

    [DataContract]
    public class Sensor
    {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public double multiplier { get; set; }
    }
    [DataContract]
    public class Settings
    {
        [DataMember]
        public string hvacMode { get; set; }
    }
    [DataContract]
    public class RemoteSensor
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public string code { get; set; }
        [DataMember]
        public bool inUse { get; set; }
        [DataMember]
        public Capability[] capability { get; set; }
    }
    [DataContract]
    public class Capability
    {
        [DataMember]
        public string id { get; set; }
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public string value { get; set; }
    }

    [DataContract]
    public class Status
    {
        [DataMember] public int code { get; set; }
        [DataMember] public string message { get; set; }
    }
}

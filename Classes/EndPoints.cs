using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using AudioSwitch.CoreAudioApi;

namespace AudioSwitch.Classes
{
    internal static class EndPoints
    {
        private static int DefaultDeviceID;
        internal static string DefaultDeviceName;

        internal static readonly MMDeviceEnumerator DeviceEnumerator;
        public static readonly MMDeviceNotifyClient NotifyClient;
        private static readonly Dictionary<int, string> DeviceIDs = new Dictionary<int, string>();
        public static readonly Dictionary<string, string> DeviceNames = new Dictionary<string, string>(); 
        private static readonly PolicyConfigClient pPolicyConfig = new PolicyConfigClient();
        
        static EndPoints()
        {
            DeviceEnumerator = new MMDeviceEnumerator();
            NotifyClient = new MMDeviceNotifyClient();
            DeviceEnumerator.RegisterEndpointNotificationCallback(NotifyClient);
        }

        internal static void RefreshDeviceList(EDataFlow renderType)
        {
            DeviceNames.Clear();
            DeviceIDs.Clear();

            var pDevices = DeviceEnumerator.EnumerateAudioEndPoints(renderType, EDeviceState.Active);
            var defDeviceID = DeviceEnumerator.GetDefaultAudioEndpoint(renderType, ERole.eMultimedia).ID;
            var devCount = pDevices.Count;
            var newCount = 0;

            for (var i = 0; i < devCount; i++)
            {
                var device = pDevices[i];
                var devID = device.ID;
                
                var devSettings = Program.settings.Device.Find(x => x.DeviceID == devID);
                if (devSettings == null || !devSettings.HideFromList)
                {
                    var devName = device.FriendlyName;
                    DeviceNames.Add(devID, devName);
                    DeviceIDs.Add(newCount, devID);
                    
                    if (devID == defDeviceID)
                    {
                        DefaultDeviceID = newCount;
                        DefaultDeviceName = devName;
                    }
                    newCount++;
                }
            }
        }

        internal static string SetPrevDefault(EDataFlow rType)
        {
            RefreshDeviceList(rType);

            if (DefaultDeviceID == 0)
                DefaultDeviceID = DeviceNames.Count - 1;
            else
                DefaultDeviceID--;

            SetDefaultDeviceByID(DefaultDeviceID);
            return DeviceNames[DeviceIDs[DefaultDeviceID]];
        }

        internal static string SetNextDefault(EDataFlow rType)
        {
            RefreshDeviceList(rType);

            if (DefaultDeviceID == DeviceNames.Count - 1)
                DefaultDeviceID = 0;
            else
                DefaultDeviceID++;

            SetDefaultDeviceByID(DefaultDeviceID);
            return DeviceNames[DeviceIDs[DefaultDeviceID]];
        }

        internal static MMDevice GetDefaultMMDevice(EDataFlow renderType)
        {
            return DeviceEnumerator.GetDefaultAudioEndpoint(renderType, ERole.eMultimedia);
        }

        internal static MMDevice GetDefaultMMDevice(EDataFlow renderType, ERole erole)
        {
            return DeviceEnumerator.GetDefaultAudioEndpoint(renderType, erole);
        }

        internal static Dictionary<MMDevice, Icon> GetAllDeviceList()
        {
            var devices = new Dictionary<MMDevice, Icon>();
            var pDevices = DeviceEnumerator.EnumerateAudioEndPoints(EDataFlow.eAll, EDeviceState.Active);
            var devCount = pDevices.Count;

            for (var i = 0; i < devCount; i++)
            {
                var device = pDevices[i];
                devices.Add(device, DeviceIcons.GetIcon(device.IconPath));
            }

            return devices;
        }

        internal static void SetDefaultDeviceByID(int devID)
        {
            SetDefaultDevice(DeviceIDs[devID]);
            DefaultDeviceID = devID;
            DefaultDeviceName = DeviceNames[DeviceIDs[devID]];
        }

        internal static bool SetDefaultDeviceByName(string devName)
        {
            foreach (var device in DeviceNames.Where(device => device.Value == devName))
            {
                SetDefaultDevice(device.Key);
                DefaultDeviceName = device.Value;
                return true;
            }
            return false;
        }

        internal static void SetDefaultDevice(string devID)
        {
            pPolicyConfig.SetDefaultEndpoint(devID, ERole.eMultimedia);
            if (Program.settings.DefaultMultimediaAndComm)
                pPolicyConfig.SetDefaultEndpoint(devID, ERole.eCommunications);
        }

        internal static void SetDefaultDevice(string devID, ERole erole)
        {
            pPolicyConfig.SetDefaultEndpoint(devID, erole);
        }
    }
}

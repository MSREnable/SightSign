namespace SightSign
{
    using System;
    using System.Collections.Generic;
    using System.Management;

    internal class PortDetails
    {
        public string Name { get; set; }
        public string PnPId { get; set; }
        public string Manufacturer { get; set; }
        public string ComName
        {
            get
            {
                var parts = Name.Split('(', ')');
                return parts.Length > 1 ? parts[1] : null;
            }
        }

        public static string FindPort()
        {
            var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity");

            var comPorts = new Dictionary<string, PortDetails>();
            foreach (var queryObj in searcher.Get())
            {
                if (queryObj["Name"] == null || !queryObj["Name"].ToString().Contains("(COM")) continue;

                var portDetails = new PortDetails
                {
                    Name = (string)queryObj["Name"],
                    PnPId = (string)queryObj["PnPDeviceID"],
                    Manufacturer = (string)queryObj["Manufacturer"]
                };

                comPorts.Add(portDetails.ComName, portDetails);
            }

            foreach (var port in comPorts.Values)
            {
                // uArm using generic windows 10 serial driver
                if (port.PnPId.Contains("FTDIBUS\\VID_0403+PID_6001") || // uArm Metal
                    port.PnPId.Contains("USB\\VID_2341&PID_0042"))
                {
                    return port.ComName;
                }
            }

            throw new Exception("Could not find COM port");
        }
    }
}
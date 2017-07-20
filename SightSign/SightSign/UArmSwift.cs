using System;
using System.Collections.Generic;
using System.Threading;
using Brief.Robotics;

namespace SightSign
{
    public class UArmSwiftPro : IArm
    {
        private readonly string _port;
        private UArmSwift _arm;

        public UArmSwiftPro()
        {
            _port = PortDetails.FindPort();
        }

        public void Connect()
        {
            _arm = new UArmSwift(_port);
            _arm.Connect();
            _arm.Mode(Mode.UniversalHolder);
        }

        public void Disconnect()
        {
            _arm.Disconnect();
            _arm = null;
        }

        public void Move(double x, double y, double z, bool scara)
        {
            // note: scara not supported
            var scale = 3.0;
            var xx = x * 70.0 * scale + 200.0; 
            var yy = y * 100.0 * scale;
            var zz = z * 20.0 + 50;
            System.Diagnostics.Debug.WriteLine($"X={xx} Y={yy} Z={zz}");
            _arm.MoveXYZ(xx, yy, zz, 5000);
        }
    }
}
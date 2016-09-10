using System;
using System.Threading;
using Microsoft.Robotics.Brief;
using Microsoft.Robotics.Tests.Reflecta;

namespace eyeSign
{
    public class UArm
    {
        private readonly string _port;
        private ReflectaClient _reflecta;
        private readonly Compiler _compiler = new Compiler();

        public UArm(string port)
        {
            _port = port;
        }

        private void Exec(int wait, string brief)
        {
            Console.WriteLine($@"EXEC BRIEF: {brief} ({wait})");
            var compiled = _compiler.EagerCompile(brief);
            try
            {
                var code = compiled.Item1;
                var frame = new byte[code.Length + 1];
                Array.Copy(code, 0, frame, 0, code.Length); // leave last byte=0, telling Brief to execute
                _reflecta.SendFrame(frame);
            }
            catch (Exception ex)
            {
                Console.WriteLine($@"Communication error: {ex.Message}");
            }
            Thread.Sleep(wait);
        }

        public void Connect()
        {
            _reflecta = new ReflectaClient(_port);
            _reflecta.ErrorReceived += (_, e) => Console.WriteLine($@"Error: {e.Message}");
            _compiler.Reset();
            _compiler.Instruction("attach", 103);
            _compiler.Instruction("detach", 104);
            _compiler.Instruction("xyz!!", 110);
            _compiler.Instruction("rtz!!", 113);
            Exec(500, "(reset)");
            Exec(100, "attach");
        }

        public void Disconnect()
        {
            Exec(100, "detach");
            _reflecta.Dispose();
            _reflecta = null;
        }

        private int Distance3D(double a0, double a1, double b0, double b1, double c0, double c1)
        {
            Func<double, double> sq = x => x * x;
            return (int)(Math.Sqrt(sq(a1 - a0) + sq(b1 - b0) + sq(c1 - c0)) * 10000);
        }

        private double _x, _y, _z;

        public void Move(double x, double y, double z, bool scara)
        {
            var dist = Distance3D(x, _x, y, _y, z, _z);
            _x = x; _y = y; _z = z;
            var wait = dist / 5.0;
            if (scara)
            {
                // in scara mode, up is base rotation
                var rr = (int)(x * 10000.0) + 22000;
                var tt = (int)(z * 650.0) + 1800;
                var zz = (int)(-y * 10000.0) + 5000;
                Exec((int)wait, $"{rr} {tt} {zz} 3000 rtz!!");
            }
            else
            {
                var xx = (int)(x * 10000.0) + 11000;
                var yy = (int)(y * 10000.0);
                var zz = (int)(z * 10000.0) + 5000;
                Exec((int)wait, $"{xx} {yy} {zz} 3000 xyz!!");
            }
        }
    }
}
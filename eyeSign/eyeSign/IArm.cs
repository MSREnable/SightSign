namespace eyeSign
{
    public interface IArm
    {
        void Connect();
        void Disconnect();
        void Move(double x, double y, double z, bool scara);
    }
}

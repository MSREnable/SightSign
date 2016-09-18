namespace eyeSign
{
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
    }
}
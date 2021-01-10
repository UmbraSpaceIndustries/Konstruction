namespace KonstructionUI
{
    public class ShipMetadata
    {
        public KonstructorMetadata KonstructorMetadata { get; private set; }
        public string Name { get; private set; }

        public ShipMetadata(
            string name,
            KonstructorMetadata konstructorMetadata)
        {
            KonstructorMetadata = konstructorMetadata;
            Name = name;
        }
    }
}

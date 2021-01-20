using UnityEngine;

namespace KonstructionUI
{
    public class ShipMetadata
    {
        public string Cost { get; private set; }
        public KonstructorMetadata KonstructorMetadata { get; private set; }
        public string Mass { get; private set; }
        public string Name { get; private set; }
        public Texture2D Thumbnail { get; private set; }

        public ShipMetadata(
            string name,
            string mass,
            string cost,
            KonstructorMetadata konstructorMetadata,
            Texture2D thumbnail)
        {
            Cost = cost;
            KonstructorMetadata = konstructorMetadata;
            Mass = mass;
            Name = name;
            Thumbnail = thumbnail;
        }
    }
}

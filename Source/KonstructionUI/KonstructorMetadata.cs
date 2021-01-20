using System.Collections.Generic;
using System.Linq;

namespace KonstructionUI
{
    public class KonstructorResourceMetadata
    {
        public double Available { get; set; }
        public string Name { get; private set; }
        public double Needed { get; private set; }

        public KonstructorResourceMetadata(
            string name,
            double available,
            double needed)
        {
            Available = available;
            Name = name;
            Needed = needed;
        }
    }

    public class KonstructorMetadata
    {
        public bool CanSpawn { get; private set; }
        public List<KonstructorResourceMetadata> Resources { get; private set; }

        public KonstructorMetadata(List<KonstructorResourceMetadata> resources)
        {
            if (resources != null)
            {
                Resources = resources;
                CanSpawn = !resources.Any(r => (r.Available - r.Needed) < 0.0001d);
            }
        }
    }
}

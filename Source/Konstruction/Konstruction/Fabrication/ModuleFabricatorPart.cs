namespace Konstruction.Fabrication
{
    public class ModuleKonFabricator : PartModule
    {
        [KSPField]
        public float massLimit = 0.05f;  //50 kg
          
        [KSPField]
        public float volLimit = 50f;    //50 Liters
    }
}

namespace Konstruction
{
    public class KonstructionScenario : ScenarioModule
    {
        public KonstructionScenario()
        {
            Instance = this;
            settings = new KonstructionPersistance();
        }

        public static KonstructionScenario Instance { get; private set; }
        public KonstructionPersistance settings { get; private set; }

        public override void OnLoad(ConfigNode gameNode)
        {
            base.OnLoad(gameNode);
            settings.Load(gameNode);
        }

        public override void OnSave(ConfigNode gameNode)
        {
            base.OnSave(gameNode);
            settings.Save(gameNode);
        }
    }

}

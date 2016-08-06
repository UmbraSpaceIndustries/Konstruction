using System.Linq;

namespace Konstruction
{
    public class ModuleServoConfig : PartModule
    {
        [KSPField]
        public string menuName;

        [KSPField]
        public string nodeConfig;

        [KSPField]
        public float defaultSpeed = 20f;

        [KSPAction("Set Config")]
        public void SetConfigAction(KSPActionParam param)
        {
            SetConfig();
        }

        [KSPEvent(guiName = "Set Config", guiActive = true, guiActiveEditor = true)]
        public void SetConfig()
        {
             //We're essentially setting goals and turning on a bunch of servos.
            var servoStrings = nodeConfig.Split(',');
            //Start by setting all servos to default.
            foreach (var servo in part.FindModulesImplementing<ModuleServo>())
            {
                servo.SetGoal(0f,defaultSpeed);
            }

            for (int i = 0; i < servoStrings.Length; i += 3)
            {
                var servoName = servoStrings[i];
                var servo = part.FindModulesImplementing<ModuleServo>().FirstOrDefault(m => m.menuName == servoName);
                if (servo != null)
                {
                    servo.SetGoal(float.Parse(servoStrings[i + 1]), float.Parse(servoStrings[i + 2]));
                }
            }
        }

        public override void OnStart(StartState state)
        {
            Events["SetConfig"].guiName = menuName;
            Actions["SetConfigAction"].guiName = menuName;
        }
    }
}
using System.Collections.Generic;
using System.Linq;

namespace Konstruction
{
    public class ModuleServoGroup : PartModule
    {
        [KSPField(isPersistant = true)] public int GroupState = 0;

        [KSPField(isPersistant = true)] public int GroupID = 0;

        public void FixedUpdate()
        {

            if (GroupState != 2)
                return;

            //Master only.
            var masterServos = part.FindModulesImplementing<ModuleServo>();

            var allGroups = new List<ModuleServoGroup>();

            if (HighLogic.LoadedSceneIsEditor)
            {
                if (EditorLogic.fetch != null)
                {
                    foreach (var p in EditorLogic.fetch.ship.parts)
                    {
                        var mods = p.FindModulesImplementing<ModuleServoGroup>();
                        if (mods != null)
                            allGroups.AddRange(mods);
                    }
                }
            }
            else
            {
                allGroups =
                    vessel.FindPartModulesImplementing<ModuleServoGroup>().ToList();
            }
            foreach (var slaveGroup in allGroups)
            {
                if (slaveGroup.GroupState != 1 || slaveGroup.GroupID != GroupID)
                    continue;

                //Slaves only
                var slaveList = slaveGroup.part.FindModulesImplementing<ModuleServo>();

                //Iterate through the master...
                foreach (var ms in masterServos)
                {
                    //Iterate all slaves
                    foreach (var slave in slaveList)
                    {
                        if (ms.menuName == slave.menuName)
                        {
                            if (slave.GroupBehavior == 0) //Same
                            {
                                slave.goalValue = ms.goalValue;
                                slave.MoveToGoal = ms.MoveToGoal;
                                slave.ServoSpeed = ms.ServoSpeed;
                            }
                            else if (slave.GroupBehavior == 1) //flip
                            {
                                slave.goalValue = -ms.goalValue;
                                slave.MoveToGoal = ms.MoveToGoal;
                                slave.ServoSpeed = -ms.ServoSpeed;
                            }
                        }
                    }
                }
            }
        }
    }
}

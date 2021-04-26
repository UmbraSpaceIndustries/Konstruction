using System;

namespace Konstruction
{
    public class ModuleCounterweight : PartModule
    {
        //Quick and hacky...
        [KSPEvent(guiActive = false, guiName = "Load Ballast")]
        public void LoadBallast()
        {
            ScreenMessages.PostScreenMessage("You load a bunch of rocks and dirt into the counterweight");
            res.amount = res.maxAmount;
            ToggleLoadBallast(false);
        }

        [KSPEvent(guiActive = false, guiName = "Unload Ballast")]
        public void UnloadBallast()
        {
            ScreenMessages.PostScreenMessage("You remove a bunch of rocks and dirt from the counterweight");
            res.amount = 0;
            ToggleLoadBallast(true);
        }

        private PartResource res;

        public override void OnStart(StartState state)
        {
             res = part.Resources[0];
            if (Math.Abs(res.amount - res.maxAmount) < ResourceUtilities.FLOAT_TOLERANCE)
            {
                ToggleLoadBallast(false);
            }
            else
            {
                ToggleLoadBallast(true);
            }
        }

        private void ToggleLoadBallast(bool isLoadable)
        {
            Events["UnloadBallast"].guiActive = !isLoadable;
            Events["LoadBallast"].guiActive = isLoadable;
            MonoUtilities.RefreshContextWindows(part);
        }
    }
}

using static GameParameters;

namespace Konstruction
{
    public class Konstruction_GameParameters : CustomParameterNode
    {
        public override bool HasPresets => false;
        public override string DisplaySection => "Konstruction";
        public override GameMode GameMode => GameMode.ANY;
        public override string Section => "Konstruction";
        public override int SectionOrder => 1;
        public override string Title => string.Empty;

        #region Custom UI parameters
        [CustomIntParameterUI(
            "#LOC_USI_ResourceTransfers_AllowedRadius_OptionTitle",
            autoPersistance = true, minValue = 50, maxValue = 1000, stepSize = 50,
            toolTip = "#LOC_USI_ResourceTransfers_AllowedRadius_Tooltip")]
        public int resourceTransferAllowedRadius = 250;
        #endregion

        #region Static accessor properties
        public static float ResourceTransferAllowedRadius
        {
            get
            {
                var options = HighLogic.CurrentGame.Parameters
                    .CustomParams<Konstruction_GameParameters>();

                return options.resourceTransferAllowedRadius;
            }
        }
        #endregion
    }
}

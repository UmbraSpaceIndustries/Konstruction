using System.Text;
using UnityEngine;

namespace Konstruction
{
    public class ModuleKonstructionHelper : PartModule
    {
        [KSPField]
        public int KonstructionPoints = 0;

        public override string GetInfo()
        {
            var output = new StringBuilder();
            output.AppendLine("Aids in EVA Construction.\n\n");
            output.AppendLine(KonstructionPoints.ToString());
            output.AppendLine(" Konstruction Point(s)"); return output.ToString();
        }
    }
}

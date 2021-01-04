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
            output.AppendLine("Aids in EVA Construction");
            return output.ToString();
        }
    }
}

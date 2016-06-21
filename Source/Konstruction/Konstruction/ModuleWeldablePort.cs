using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using PreFlightTests;
using TestScripts;

namespace Konstruction
{
    public class ModuleMultiWelder : PartModule
    {
        [KSPEvent(guiActive = true, guiActiveEditor = false,
            guiName = "Weld All Parts")]
        public void WeldAllParts()
        {
            var pList = new List<Part>();
            if (part.parent.Modules.Contains("ModuleWeldablePort"))
                pList.Add(part.parent);

            pList.AddRange(part.children.Where(c => c.Modules.Contains("ModuleWeldablePort")));

            for (int i = pList.Count - 1; i >= 0; i--)
            {
                var mod = pList[i].FindModuleImplementing<ModuleWeldablePort>();
                if (mod != null)
                    mod.WeldParts();
            }
        }

    }

    public class ModuleWeldablePort : PartModule
    {
        [KSPEvent(active = true, guiActive = true, guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = 10, guiName = "Weld In Place")]
        public void WeldParts()
        {
            MergeParts(false,false);
        }

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = 10, guiName = "Compress Parts")]
        public void CompressParts()
        {
            MergeParts(true,false);
        }

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = 10, guiName = "Compress with Rotation")]
        public void ConstructParts()
        {
            MergeParts(true,true);
        }

        public override void OnStart(StartState state)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            try
            {
                Events["CompressParts"].guiActive = true;
                Events["CompressParts"].guiActiveUnfocused = true;
                Events["CompressParts"].active = true;

                Events["ConstructParts"].guiActive = true;
                Events["ConstructParts"].guiActiveUnfocused = true;
                Events["ConstructParts"].active = true;
            }
            catch (Exception ex)
            {
                Debug.Log(String.Format("[ModuleWeldablePart] Error {0} in OnStart", ex.Message));
            }
        }

        private void MergeParts(bool compress, bool fixRotation)
        {
            if (vessel.rootPart == this)
            {
                ScreenMessages.PostScreenMessage("You cannot weld the root part!");
                return;
            }

            var wData = LoadWeldingData();
            if (wData == null)
                return;

            PerformWeld(wData, compress, fixRotation);
        }

        private bool IsWeldablePort(Part p)
        {
            if (p == null)
                return false;

            if (!p.FindModulesImplementing<ModuleDockingNode>().Any())
                return false;

            if (!p.FindModulesImplementing<ModuleWeldablePort>().Any())
                return false;

            return true;
        }

        private Part FindAttachedPart(Part p, Part xp)
        {
            print(string.Format("Finding attached part for {0} but excluding {1}", p.partInfo.title, xp.partInfo.title));
            print(string.Format("Part {0} has {1} attachment node(s)", p.partInfo.title, p.attachNodes.Count));
            foreach (var an in p.attachNodes)
            {
                print(string.Format("Looking at node {0}", an.id));
                if (an.attachedPart != null && an.attachedPart != xp)
                {
                    print(string.Format("Returning {0}", an.attachedPart.partInfo.title));
                    return an.attachedPart;
                }
            }

            print(string.Format("Part {0} parent part is {1}", p.partInfo.title, p.parent));
            print(string.Format("Part {0} has {1} children", p.partInfo.title, p.children.Count));


            if (p.parent != null)
                return p.parent;

            return p.children.Count() == 1 ? p.children[0] : null;
        }

        private WeldingData LoadWeldingData(bool silent = false,bool retry = false)
        {
            /**********************
             * 
             *   LPA==DPA-><-DPB==LPB
             * 
             *         LPA==LPB
             * 
             **********************/
            var wData = new WeldingData();
            if (IsWeldablePort(part.parent))
            {
                wData.DockingPortA = part.parent;
                wData.DockingPortB = part;
            }
            else if (part.children != null && part.children.Count > 0)
            {
                foreach (var p in part.children.Where(IsWeldablePort))
                {
                    wData.DockingPortA = part;
                    wData.DockingPortB = p;
                    break;
                }
            }

            //Check if either are null
            if (wData.DockingPortA == null || wData.DockingPortB == null)
            {
                if (!silent)
                    ScreenMessages.PostScreenMessage("Must weld two connected, weldable docking ports!");
                return null;
            }

            wData.LinkedPartA = FindAttachedPart(wData.DockingPortA, wData.DockingPortB);
            wData.LinkedPartB = FindAttachedPart(wData.DockingPortB, wData.DockingPortA);

            if (wData.LinkedPartA == null || wData.LinkedPartB == null)
            {
                if (!silent)
                    ScreenMessages.PostScreenMessage("Both weldable ports must be connected to another part!");
                return null;
            }

            if (wData.DockingPortA == vessel.rootPart)
            {
                if (!silent)
                    ScreenMessages.PostScreenMessage("This port is the root part!  Cancelling");
                return null;
            }

            if (wData.DockingPortB == vessel.rootPart)
            {
                if (!silent)
                    ScreenMessages.PostScreenMessage("Attempting to weld to root part!  Cancelling");
                return null;
            }

            return wData;
        }


        private Vector3 GetOffset(WeldingData wData)
        {
            var totalThickness = 0f;
            var offset =
                  wData.LinkedPartA.transform.localPosition
                - wData.LinkedPartB.transform.localPosition;

            offset.Normalize();

            totalThickness += NodeUtilities.GetPartThickness(wData.DockingPortA);
            totalThickness += NodeUtilities.GetPartThickness(wData.DockingPortB);

            offset *= totalThickness;

            return offset;
        }

        private Vector3 GetRotation(WeldingData wData)
        {
            var offset = Quaternion.Inverse(wData.DockingPortA.transform.localRotation)
                * wData.DockingPortB.transform.localRotation;

            var e = offset.eulerAngles;
            var angleY = new Vector3(0,e.y,0);
            return angleY;
        }


        private void PerformWeld(WeldingData wData, bool compress, bool fixRotation)
        {
            var offset = GetOffset(wData);
            var rotation = GetRotation(wData);

            var nodeA = NodeUtilities.GetLinkingNode(wData.LinkedPartA, wData.DockingPortA);
            var nodeB = NodeUtilities.GetLinkingNode(wData.LinkedPartB, wData.DockingPortB);



            NodeUtilities.DetachPart(wData.DockingPortA);
            NodeUtilities.DetachPart(wData.DockingPortB);

            NodeUtilities.SwapLinks(
                wData.LinkedPartA,
                wData.DockingPortA,
                wData.LinkedPartB);
            NodeUtilities.SwapLinks(
                wData.LinkedPartB,
                wData.DockingPortB,
                wData.LinkedPartA);

            wData.DockingPortB.SetCollisionIgnores();
            wData.DockingPortA.SetCollisionIgnores();

            NodeUtilities.SpawnStructures(wData.LinkedPartA, nodeA);
            NodeUtilities.SpawnStructures(wData.LinkedPartB, nodeB);

            if (fixRotation)
                wData.LinkedPartB.transform.Rotate(rotation);

            if (compress)
                NodeUtilities.MovePart(wData.LinkedPartB, offset);



            PartJoint newJoint = PartJoint.Create(
                wData.LinkedPartB,
                wData.LinkedPartA,
                nodeB,
                nodeA,
                AttachModes.STACK);

            wData.LinkedPartB.attachJoint = newJoint;

            SoftExplode(wData.DockingPortA);
            SoftExplode(wData.DockingPortB);
        }

        private static void SoftExplode(Part thisPart)
        {
            thisPart.explosionPotential = 0;
            thisPart.explode();
        }
    }
}

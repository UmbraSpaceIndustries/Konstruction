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
    public class ModuleWeldablePort : PartModule
    {
        [KSPEvent(active = true, guiActive = true, guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = 10, guiName = "Compress Parts")]
        public void CompressParts()
        {
            MergeParts(true,false);
        }

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = 10, guiName = "Compress Parts (Rotate)")]
        public void CompressPartsRot()
        {
            MergeParts(true, true);
        }

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = 10, guiName = "Reset Acquire")]
        public void ResetAcquire()
        {
            //Clear the 'Other Node' on both ports
            if (dock.otherNode != null)
            {
                dock.otherNode.otherNode = null;
                dock.otherNode = null;
            }
        }

        [KSPEvent(active = true, guiActive = true, guiActiveEditor = false, guiActiveUnfocused = true, unfocusedRange = 10, guiName = "Multi-Weld")]
        public void MultiWeld()
        {
            //we're going to start by welding every other port other than this.
            var allPorts = vessel.FindPartModulesImplementing<ModuleWeldablePort>();
            var weldPorts = new List<Part>();
            weldPorts.Add(part);
            foreach (var p in allPorts)
            {
                bool addPart = true;
                var thisPart = p.part;
                //Skip anyone already in a set.
                foreach (var w in weldPorts)
                {
                    //Skip ourselves
                    if (thisPart == w || thisPart == w.parent || w.children.Contains(thisPart))
                        addPart = false;
                }

                if(addPart)
                    weldPorts.Add(thisPart);
            }
            //Now go in reverse, start exploding!  We'll end with this part.
            for (int i = weldPorts.Count; i -- > 0;)
            {
                var dp = weldPorts[i].FindModuleImplementing<ModuleWeldablePort>();
                dp.MergeParts(true,false);
            }
        }

        [KSPField(guiName = "Port Force", isPersistant = true, guiActive = true, guiActiveEditor = false), UI_FloatRange(stepIncrement = 0.5f, maxValue = 20f, minValue = 0f)]
        public float portForce = 2;

        [KSPField(guiName = "Port Torque", isPersistant = true, guiActive = true, guiActiveEditor = false), UI_FloatRange(stepIncrement = 0.5f, maxValue = 20, minValue = 0f)]
        public float portTorque = 2;

        [KSPField(guiName = "Port Roll", isPersistant = true, guiActive = true, guiActiveEditor = false), UI_FloatRange(stepIncrement = 0.5f, maxValue = 20, minValue = 0f)]
        public float portRoll = 0;

        [KSPField(guiName = "Port Range", isPersistant = true, guiActive = true, guiActiveEditor = false), UI_FloatRange(stepIncrement = 0.1f, maxValue = 5f, minValue = 0f)]
        public float portRange = 0.5f;

        [KSPField(guiName = "Angle", isPersistant = true, guiActive = true, guiActiveEditor = false), UI_FloatRange(stepIncrement = 15f, maxValue = 180f, minValue = 0f)]
        public float portAngle = 0f;

        [KSPField(guiName = "Snap", isPersistant = true, guiActive = true, guiActiveEditor = false), UI_Toggle(disabledText = "Off", enabledText = "On")]
        public bool portSnap = true;

        private ModuleDockingNode dock;

        public void Start()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            try
            {
                Events["CompressParts"].guiActive = true;
                Events["CompressParts"].guiActiveUnfocused = true;
                Events["CompressParts"].active = true;
                dock = part.FindModuleImplementing<ModuleDockingNode>();
            }
            catch (Exception ex)
            {
                Debug.Log(String.Format("[ModuleWeldablePart] Error {0} in OnStart", ex.Message));
            }
        }

        public void Update()
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            dock.acquireForce = portForce;
            dock.acquireTorque = portTorque;
            dock.acquireTorqueRoll = portRoll;
            dock.acquireRange = portRange;
            dock.snapOffset = portAngle;
            if (portSnap)
                dock.captureMinRollDot = 0.999f;
            else
                dock.captureMinRollDot = float.MinValue;
            dock.snapRotation = portSnap;
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

        private bool ResetRootPart()
        {
            if (vessel.parts.Count < 4)
            {
                ScreenMessages.PostScreenMessage("The vessel must have at least four parts to perform welding!");
                return false;
            }

            var potParts = new List<Part>();

            var count = part.children.Count;
            for (int i = 0; i < count; ++i)
            {
                potParts.Add(part.children[i]);
                potParts.AddRange(part.children[i].children);
            }

            //Now roll through the potParts and find our match.  This covers
            //both children and grandchildren... so we had better be able to find a part!

            var pCount = potParts.Count;
            for (int i = 0; i < pCount; ++i)
            {
                var c = potParts[i];
                //Can't be another weldable port...
                if (c.FindModuleImplementing<ModuleWeldablePort>() != null)
                    continue;

                //Winner!  This is our new root
                c.SetHierarchyRoot(vessel.rootPart);
                return true;
            }
            //No luck...
            return false;
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
            var objA = new GameObject();
            var objB = new GameObject();

            var transformA = objA.transform;
            var transformB = objB.transform;

            transformA.localPosition = wData.DockingPortA.transform.localPosition;
            transformB.localPosition = wData.DockingPortB.transform.localPosition;

            var offset =
                transformA.localPosition - transformB.localPosition;

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
            var oAngle = offset.eulerAngles;           
            //Add a 180 x-flip.
            return new Vector3(oAngle.x, oAngle.y, CorrectAngle(oAngle.z - 180));
        }

        private float CorrectAngle(float angle )
        {
            var a = angle;
            if (a < 0)
                a += 360;
            return a;
        }

        private void PerformWeld(WeldingData wData, bool compress, bool fixRotation)
        {
            var nodeA = NodeUtilities.GetLinkingNode(wData.LinkedPartA, wData.DockingPortA);
            var nodeB = NodeUtilities.GetLinkingNode(wData.LinkedPartB, wData.DockingPortB);

            var offset = GetOffset(wData);
            var rotation = GetRotation(wData);

            if (fixRotation)
                wData.LinkedPartB.transform.Rotate(rotation,Space.Self);

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
            thisPart.explosionPotential = 0.1f;
            thisPart.explode();
        }
    }
}

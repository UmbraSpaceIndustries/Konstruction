using System;
using UnityEngine;

namespace Konstruction
{
    public class ModulePiston: PartModule
    {
        [KSPField]
        public string pistonAnimationName = "Deploy";

        [KSPField(isPersistant = true)]
        public float aniTime = 0f;

        [KSPField]
        public int LayerId = 2;

        [KSPField]
        public float stepSize = .05f;

        [KSPField]
        public float distancePerStep = 0.01f;

        private float curTime = 0f;

        [KSPAction("Extend Piston")]
        public void ExtendAction(KSPActionParam param)
        {
            ExtendPiston();
        }


        [KSPAction("Retract Piston")]
        public void RetractAction(KSPActionParam param)
        {
            RetractPiston();
        }

        public Animation PistonAnimation
        {
            get
            {
                return part.FindModelAnimators(pistonAnimationName)[0];
            }
        }

        [KSPEvent(guiName = "Extend", guiActive = true, externalToEVAOnly = true, guiActiveEditor = true, active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void ExtendPiston()
        {
            if (aniTime < 1f)
            {
                curTime += stepSize;
                if (curTime > 1f)
                    curTime = 1f;
            }
        }

        [KSPEvent(guiName = "Retract", guiActive = true, externalToEVAOnly = true, guiActiveEditor = false, active = true, guiActiveUnfocused = true, unfocusedRange = 3.0f)]
        public void RetractPiston()
        {
            if (aniTime > 0f)
            {
                curTime -= stepSize;
                if (curTime < 0f)
                    curTime = 0f;
            }
        }
        public override void OnStart(StartState state)
        {
            try
            {
                PistonAnimation[pistonAnimationName].layer = LayerId;
            }
            catch (Exception ex)
            {
                print("ERROR IN ModulePiston - " + ex.Message);
            }
        }

        public void FixedUpdate()
        {
            if (!PistonAnimation.IsPlaying(pistonAnimationName))
            {
                PistonAnimation[pistonAnimationName].speed = 0f;
                PistonAnimation.Play();
            }

            //Is there a diff between the current step and the animation step?
            if (curTime != aniTime)
            {
                //If so...
                var child = part.children[0];

                var offset =
                      part.transform.localPosition
                    - child.transform.localPosition;

                offset.Normalize();

                var diff = 1 + stepSize / (curTime - aniTime);
                offset *= diff;

                var nodeA = NodeUtilities.GetLinkingNode(part,child);
                var nodeB = NodeUtilities.GetLinkingNode(child,part);
 
                NodeUtilities.DetachPart(child);

                PartJoint newJoint = PartJoint.Create(
                    child,
                    part,
                    nodeB,
                    nodeA,
                    AttachModes.STACK);

                child.attachJoint = newJoint;

                // Move the parts
                NodeUtilities.MovePart(child,offset);

                // Set the animation's normalized time.
                PistonAnimation[pistonAnimationName].normalizedTime = curTime;

                aniTime = curTime;
            }
        }
    }
}
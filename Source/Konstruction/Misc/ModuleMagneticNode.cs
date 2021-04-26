using System;
using System.Collections.Generic;
using UnityEngine;

namespace Konstruction
{
    public class ModuleMagneticNode : PartModule
    {
        [KSPField]
        public string targetPartName = "PAL.MagPad";

        [KSPField]
        public string forceTransformName = "Pad0";

        [KSPField]
        public float recoil = -.25f;

        [KSPField]
        public float raycastRange = 10f;

        [KSPField]
        public string nodeList = "";

        [KSPField]
        public float baseForce = 0.5f;

        [KSPField]
        public float stopDistance = 0.1f;

        [KSPField]
        public float powerCost = 0.01f;

        [KSPField]
        public bool partsOnly =true;

        [KSPField(guiName = "Magnet Strength", isPersistant = true, guiActive = true, guiActiveEditor = false), UI_FloatRange(stepIncrement = 1f, maxValue = 100f, minValue = 0f)]
        public float magPercent = 0f;

        [KSPField(isPersistant = true)]
        public bool magnetIsEnabled = false;

        [KSPField(isPersistant = true)]
        public bool targetOnly = false;


        private List<Transform> tNode;
        private IResourceBroker broker;

        [KSPAction("Increase Power")]
        public void IncreasePowerAction(KSPActionParam param)
        {
            magPercent += 10;
            if (magPercent >= 100)
                magPercent = 100;
        }

        [KSPAction("Decrease Power")]
        public void DecreasePowerAction(KSPActionParam param)
        {
            magPercent -= 10;
            if (magPercent <= 0)
                magPercent = 0;
        }

        [KSPAction("Toggle Magnets")]
        public void ToggleMagnetAction(KSPActionParam param)
        {
            ToggleMagnet(!magnetIsEnabled);
        }


        [KSPEvent(guiActive = true, guiName = "Enable Magnet")]
        public void EnableMagnet()
        {
            ToggleMagnet(true);
        }


        [KSPEvent(guiActive = true, guiName = "Disable Magnet")]
        public void DisableMagnet()
        {
            ToggleMagnet(false);
        }

        [KSPEvent(guiActive = true, guiName = "Enable Targeting")]
        public void EnableTargeting()
        {
            ToggleTarget(true);
        }


        [KSPEvent(guiActive = true, guiName = "Disable Targeting")]
        public void DisableTargeting()
        {
            ToggleTarget(false);
        }

        private Transform _forceTransform;

        public override void OnStart(StartState state)
        {
            tNode = new List<Transform>();
            var nodes = nodeList.Split(',');
            foreach (var n in nodes)
            {
                tNode.Add(part.FindModelTransform(n));
            }
            _forceTransform = part.FindModelTransform(forceTransformName);
            ToggleMagnet(magnetIsEnabled);
            ToggleTarget(targetOnly);
            broker = new ResourceBroker();
        }

        private void ToggleMagnet(bool state)
        {
            magnetIsEnabled = state;
            Events["EnableMagnet"].guiActive = !state;
            Events["DisableMagnet"].guiActive = state;
            MonoUtilities.RefreshContextWindows(part);
        }

        private void ToggleTarget(bool state)
        {
            targetOnly = state;
            Events["EnableTargeting"].guiActive = !state;
            Events["DisableTargeting"].guiActive = state;
            MonoUtilities.RefreshContextWindows(part);
        }

        private bool MagnetHasPower()
        {
            var ecCost = powerCost*magPercent;
            if (broker.AmountAvailable(part, "ElectricCharge", TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL) >= ecCost)
            {
                broker.RequestResource(part, "ElectricCharge", ecCost, TimeWarp.fixedDeltaTime, ResourceFlowMode.ALL_VESSEL);
                return true;
            }
            else
            {
                //Auto-shutdown
                ScreenMessages.PostScreenMessage("Shutting down magnet - insufficient power!");
                ToggleMagnet(false);
                return false;
            }
        }


        public void FixedUpdate()
        {
            if (!magnetIsEnabled)
                return;

            if (!MagnetHasPower())
                return;

            var tCount = tNode.Count;
            foreach (var pos in tNode)
            {
                RaycastHit hitInfo;
                Ray ray = new Ray(pos.position, pos.up);
                float speedMult = 1f;
                int mask = 0;
                if (partsOnly)
                    mask = 19;

                if (Physics.Raycast(ray, out hitInfo, raycastRange, mask))
                {
                    var speed = baseForce*magPercent*speedMult/tCount;
                    var hitObj = hitInfo.collider.gameObject;
                   // if (PushToTarget(pos.gameObject, hitObj, stopDistance, speed))
                        //break;
                    PushToTarget(_forceTransform, hitObj.gameObject.transform, stopDistance, speed);
                }
            }
        }

        bool PushToTarget(Transform target, Transform source, float distanceToStop, float speed)
        {
            var direction = Vector3.zero;
            var distance = Vector3.Distance(source.position, target.position);
            if (distance > distanceToStop)
            {
                direction = target.position - source.position;
                var p = source.GetComponentInParent<Part>();
                var mass = 1f;
                if (p != null)
                    mass = p.mass;

                if (targetOnly)
                {
                    if (p == null)
                        return false;

                    if (p.name != targetPartName)
                        return false;
                }

                var rbSource = source.GetComponentInParent<Rigidbody>();
                var rbTarget = target.GetComponentInParent<Rigidbody>();
                if (rbTarget != null)
                {
                    rbTarget.AddForce(direction.normalized * speed * recoil * mass, ForceMode.Force);
                }
                if (rbSource != null)
                {
                    rbSource.AddForce(direction.normalized * speed * mass, ForceMode.Force);
                    return true;
                }
            }
            return false;
        }
    }
}
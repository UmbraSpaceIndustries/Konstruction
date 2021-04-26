using UnityEngine;

namespace Konstruction
{
    public class ModuleGroundAnchor : PartModule
    {
        [KSPField] public string anchorTransform = "anchor";

        [KSPField] public float impactRange = 1f;

        [KSPField]
        public float offset = 10f;

        [KSPField]
        public float anchorMass = 1000f;

        private Transform anchorPoint;
        private bool isAnchored;
        private float oldMass;
        private Vector3 oldOffset;

        public override void OnStart(StartState state)
        {
            anchorPoint = part.FindModelTransform(anchorTransform);
        }

        [KSPEvent(guiActive = true, guiName = "Anchor")]
        private void Anchor()
        {
            isAnchored = true;
            oldMass = part.mass;
            oldOffset = part.CoMOffset;
            part.mass = anchorMass;
            part.CoMOffset = new Vector3(0,offset,0);
            //part.GetComponent<Rigidbody>().isKinematic = true;
            FXMonger.Explode(part, anchorPoint.position, 0f);
            Events["Anchor"].guiActive = false;
            Events["UnAnchor"].guiActive = true;
        }

        [KSPEvent(guiActive = false, guiName = "UnAnchor")]
        private void UnAnchor()
        {
            isAnchored = false;
            part.mass = oldMass;
            part.CoMOffset = oldOffset;
            //part.GetComponent<Rigidbody>().isKinematic = false;
            FXMonger.Explode(part, anchorPoint.position, 0f);
            Events["Anchor"].guiActive = true;
            Events["UnAnchor"].guiActive = false;
        }


        public void FixedUpdate()
        {
            //if (vessel.LandedOrSplashed)
            //{
            //    if (isAnchored)
            //    {
            //        UnAnchor();
            //    }
            //    return;
            //}

            RaycastHit hitInfo;
            Ray ray = new Ray(anchorPoint.position, anchorPoint.up);
            var mask = 1 << 15;
            if (Physics.Raycast(ray, out hitInfo, impactRange, mask))
            {
                //var rbPart = part.GetComponentInParent<Rigidbody>();
                //if (rbPart != null)
                //{
                //    rbPart.AddForceAtPosition(anchorPoint.up * forceAmount, anchorPoint.position, ForceMode.Force);
                //    rbPart.angularDrag = forceAmount;
                //    //rbPart.angularVelocity = new Vector3(0f, rbPart.angularVelocity.y, 0f);
                //    //rbPart.velocity = new Vector3(0f,rbPart.velocity.y,0f);
                //}
                if (!isAnchored)
                {
                    Anchor();
                }
            }
            else
            {
                if (isAnchored)
                    UnAnchor();
            }
        }
    }
}
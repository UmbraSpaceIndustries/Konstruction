using UnityEngine;

namespace Konstruction
{
    public class ModuleGrippyWheel : PartModule
    {
        [KSPField]
        public string stickyForce;

        [KSPField]
        public float topSpeed;


        private Vector3 baseForce;

        public override void OnStart(StartState state)
        {
            var vectors = stickyForce.Split(',');
            if (vectors.Length == 3)
            {
                baseForce = new Vector3(float.Parse(vectors[0]), float.Parse(vectors[0]), float.Parse(vectors[0]));
            }
        }


        public override void OnFixedUpdate()
        {
            if (!vessel.Landed)
                return;

            var speedPercent = (float)vessel.horizontalSrfSpeed/topSpeed;
            var gravPercent = 1f/(float)vessel.mainBody.gravParameter;

            var newForce = new Vector3(
                baseForce.x * speedPercent * gravPercent,
                baseForce.y * speedPercent * gravPercent,
                baseForce.z * speedPercent * gravPercent
                );

            part.GetComponent<Rigidbody>().AddForce(newForce);
        }
    }
}
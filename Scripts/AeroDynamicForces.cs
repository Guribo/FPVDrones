using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Guribo.FPVDrones.Scripts
{
    public class AeroDynamicForces : UdonSharpBehaviour
    {
        public Rigidbody affectedRigidBody;

        // Helper opbjects to calculate area of lift/drag

        // temporary solution for lift/drag coefficients
        public float lift = 2; // liftmultiplier * density/2
        public float drag = 0.2f; // dragmultiplier * density/2


        // e.g. wind (world direction)
        public Vector3 additionalVelocity;

        public float additionalLiftFactor = 1f;
        public float additionalDragFactor = 1f;

        public AnimationCurve liftCurve;
        public AnimationCurve dragCurve;


        // area of lift/drag
        public float wingArea;

        public bool debug = false;

        public bool ownerOnly = false;

        private const float MaxForce = 100000;

        private float _maxRelativeForce;

        public float currentLiftForce;
        public float currentDragForce;

        public Vector3 currentLiftDirection;
        public Vector3 currentDragDirection;
        public Vector3 currentVelocity;

        void Start()
        {
            // temporary wing area calculation (A = a * b)

            _maxRelativeForce = MaxForce * wingArea;
        }

        void FixedUpdate()
        {
            var vrcPlayerApi = Networking.LocalPlayer;
            if (vrcPlayerApi != null && ownerOnly && !vrcPlayerApi.IsOwner(gameObject)) return;


            var transformPosition = transform.position;
            // current Velocity in World space + wind/turbulences
            currentVelocity = affectedRigidBody.GetPointVelocity(transformPosition) + additionalVelocity;
            var currVelSqrMagnitude = currentVelocity.sqrMagnitude;
            if (currVelSqrMagnitude < 0.0001f)
            {
                return;
            }

            // current Movement Direction in World space
            var currentMovementDirection = currentVelocity.normalized;

            var transformUp = transform.up;

            // Movement Right-Direction Vector (from Movement Direction and Aircraft Up-Direction)
            var rightMovementDirection = Vector3.Cross(currentMovementDirection, transformUp).normalized;

            // Movement Up-Direction Vector (from Movement Direction and Movement Right-Direction)
            currentLiftDirection = Vector3.Cross(currentMovementDirection, rightMovementDirection).normalized;

            var dotAngleOfAttack = Vector3.Dot(currentMovementDirection, transformUp);

            var angleOfAttack0To90 = Mathf.Acos(Mathf.Clamp01(Mathf.Abs(dotAngleOfAttack))) * Mathf.Rad2Deg;

            // cl: temporary lift Coefficient from angle of attack
            var liftCoefficient = liftCurve.Evaluate(angleOfAttack0To90);

            // Wing Lift
            var liftForce = Mathf.Sign(dotAngleOfAttack) * liftCoefficient *
                            (lift * additionalLiftFactor)
                            * 1.2041f * 0.5f * currVelSqrMagnitude *
                            wingArea; // cl * density / 2 * v² * A (lift = multiplier * density/2)


            currentLiftForce = Mathf.Clamp(liftForce, -_maxRelativeForce, _maxRelativeForce);

            if (debug)
            {
                Debug.Log("Lift: " + lift + " LiftForce: " + liftForce + "/" + _maxRelativeForce + " WingArea: " +
                          wingArea + " velocity: " +
                          currentVelocity.magnitude);
            }


            // direction of air resistance
            currentDragDirection = -currentMovementDirection;

            // Wing air resistance
            var angleOfDrag0To90 = 90f - angleOfAttack0To90;
            // cw: temporary resistance Coefficient from angle of attack
            float resistanceCoefficient = dragCurve.Evaluate(angleOfDrag0To90);

            // cw * density / 2 * v² * A (drag = multiplier * density/2)
            float resistanceForceMagnitude = resistanceCoefficient *
                                             (drag * additionalDragFactor)
                                             * currVelSqrMagnitude *
                                             wingArea;

            // Debug.Log($"AOT {Mathf.Sign(dotAngleOfAttack) * angleOfAttack0To90} AOD {angleOfDrag0To90}");


            // v = a*t
            // <=> v/t = a
            currentDragForce = Mathf.Clamp(resistanceForceMagnitude, -_maxRelativeForce, _maxRelativeForce);
            affectedRigidBody.AddForceAtPosition(currentDragDirection * currentDragForce
                                                 + currentLiftDirection * currentLiftForce,
                transformPosition,
                ForceMode.Force);
        }
    }
}
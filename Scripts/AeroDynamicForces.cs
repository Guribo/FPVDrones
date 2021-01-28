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


        // temporary solution for lift/drag coefficients
        public float lift = 1f;
        public float drag = 1f;


        // e.g. wind (world direction)
        public Vector3 additionalVelocity;

        public float additionalLiftFactor = 1f;
        public float additionalDragFactor = 1f;

        public AnimationCurve liftCurve;
        public AnimationCurve dragCurve;

        // area of lift/drag
        public float wingArea;

        public bool ownerOnly = true;

        private const float MaxForce = 100000;

        private float _maxRelativeForce;

        public float currentLiftForce;
        public float currentDragForce;

        public Vector3 currentLiftDirection;
        public Vector3 currentDragDirection;
        public Vector3 currentVelocity;

        private VRCPlayerApi _localPlayerApi;

        public void Start()
        {
            // temporary wing area calculation (A = a * b)

            _maxRelativeForce = MaxForce * wingArea;
            _localPlayerApi = Networking.LocalPlayer;

            if (!affectedRigidBody)
            {
                affectedRigidBody = FindParentRigidBody(transform, false);
            }

            Assert(affectedRigidBody, "Rigidbody exists in parents");
            Assert(liftCurve != null, "liftCurve is valid");
            Assert(dragCurve != null, "dragCurve is valid");
        }


        public void FixedUpdate()
        {
            if (_localPlayerApi == null
                || liftCurve == null
                || dragCurve == null
                || !affectedRigidBody
                || affectedRigidBody.IsSleeping())
            {
                return;
            }

            if (ownerOnly && !_localPlayerApi.IsOwner(gameObject)) return;


            var transformPosition = transform.position;
            // current Velocity in World space + wind/turbulences
            currentVelocity = affectedRigidBody.GetPointVelocity(transformPosition) + additionalVelocity;
            var currVelSqrMagnitude = currentVelocity.sqrMagnitude;
            if (currVelSqrMagnitude < 0.001f)
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

            // direction of air resistance
            currentDragDirection = -currentMovementDirection;

            // Wing air resistance
            var angleOfDrag0To90 = 90f - angleOfAttack0To90;
            // cw: temporary resistance Coefficient from angle of attack
            var resistanceCoefficient = dragCurve.Evaluate(angleOfDrag0To90);

            // cw * density / 2 * v² * A (drag = multiplier * density/2)
            var resistanceForceMagnitude = resistanceCoefficient *
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

        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"Assertion failed : '{GetType()} : {message}'", this);
            }
        }

        /// <summary>
        /// searches in the parents for either the first Rigidbody
        /// or the last one (the one which is closest to the scene root)
        /// </summary>
        /// <param name="start">start transform (inclusive)</param>
        /// <param name="returnFirstResult">if true returns the first rigidbody that 
        /// is encountered while moving up the tree</param>
        /// <returns>the found rigidbody or null if none was found</returns>
        private Rigidbody FindParentRigidBody(Transform start, bool returnFirstResult)
        {
            if (!start)
            {
                return null;
            }

            Rigidbody result = null;
            for (var next = start; next; next = next.parent)
            {
                var rigidbodies = next.GetComponents<Rigidbody>();
                if (rigidbodies == null || rigidbodies.Length == 0)
                {
                    continue;
                }

                var foundRigidBody = rigidbodies[0];
                if (!foundRigidBody)
                {
                    continue;
                }

                if (returnFirstResult)
                {
                    return foundRigidBody;
                }

                result = foundRigidBody;
            }

            return result;
        }
    }
}
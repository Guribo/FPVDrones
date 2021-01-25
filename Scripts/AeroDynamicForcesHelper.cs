#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System;
using Guribo.UdonUtils.Scripts;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using VRC.Udon;

namespace Guribo.FPVDrones.Scripts
{
    [RequireComponent(typeof(UdonBehaviour))]
    [ExecuteInEditMode]
    public class AeroDynamicForcesHelper : MonoBehaviour
    {
        public Rigidbody affectedRigidBody;

        public float width = 1;
        public float length = 1;
        public float lift = 1f;
        public float drag = 0.1f;

        public string wingAreaName = "wingArea";
        public string affectedRigidBodyName = "affectedRigidBody";
        public string liftName = "lift";
        public string dragName = "drag";

        public string currentLiftForceName = "currentLiftForce";
        public string currentDragForceName = "currentDragForce";
        public string currentVelocityName = "currentVelocity";

        public string liftCurveName = "liftCurve";
        public string dragCurveName = "dragCurve";

        public string currentLiftDirectionName = "currentLiftDirection";
        public string currentDragDirectionName = "currentDragDirection";

        public AnimationCurve liftCurve = AnimationCurve.Linear(0, 0, 90, 0);
        public AnimationCurve dragCurve = AnimationCurve.Linear(0, 0, 90, 1);
        public AnimationCurve liftDragRatioCurve;


        private UdonBehaviour _aeroDynamicForces;

        public void Awake()
        {
            _aeroDynamicForces = GetComponent<UdonBehaviour>();
            Debug.Assert(_aeroDynamicForces, gameObject);
        }

        public void Apply()
        {
            liftDragRatioCurve = AnimationCurve.Linear(0, 0, 90f, 0);

            for (int i = 0; i < 91; i++)
            {
                var time = i;
                var timeLift = liftCurve.Evaluate(time);
                var timeDrag = dragCurve.Evaluate(time);
                if (timeDrag > 0)
                {
                    liftDragRatioCurve.AddKey(time, timeLift / timeDrag);
                }
            }

            if (!_aeroDynamicForces) return;
            try
            {
                _aeroDynamicForces.SetInspectorVariable(wingAreaName, width * length);
                _aeroDynamicForces.SetInspectorVariable(liftName, lift);
                _aeroDynamicForces.SetInspectorVariable(dragName, drag);
                _aeroDynamicForces.SetInspectorVariable(liftCurveName, liftCurve);
                _aeroDynamicForces.SetInspectorVariable(dragCurveName, dragCurve);
            }
            catch (Exception e)
            {
                Debug.LogException(e, gameObject);
            }
        }

        private void OnDrawGizmos()
        {
            if (!_aeroDynamicForces) return;

            var aeroTransform = _aeroDynamicForces.transform;
            var position = aeroTransform.position;
            var rotation = aeroTransform.rotation;

            // draw outline of lift area
            var forward = rotation * Vector3.forward;
            var right = rotation * Vector3.right;

            var halfLength = (length * 0.5f) * forward;
            var halfWidth = (width * 0.5f) * right;

            var frontLeft = position + halfLength - halfWidth;
            var frontRight = position + halfLength + halfWidth;
            var backLeft = position - halfLength - halfWidth;
            var backRight = position - halfLength + halfWidth;


            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(frontLeft, frontRight);
            Gizmos.DrawLine(frontRight, backRight);
            Gizmos.DrawLine(backRight, backLeft);
            Gizmos.DrawLine(backLeft, frontLeft);

            // draw up and forward arrow
            var area = width * length;
            var up = aeroTransform.up;
            Handles.color = Color.cyan;
            Handles.Label(position + up * area, $" {area} m²");

            Gizmos.color = Color.green;
            GizmoUtils.DrawArrow(position, up, area);

            Gizmos.color = Color.blue;
            GizmoUtils.DrawArrow(position, aeroTransform.forward, area);

            if (Application.IsPlaying(gameObject))
            {
                var liftForce = (float) _aeroDynamicForces.GetProgramVariable(currentLiftForceName);
                var liftDirection = (Vector3) _aeroDynamicForces.GetProgramVariable(currentLiftDirectionName);
                var dragForce = (float) _aeroDynamicForces.GetProgramVariable(currentDragForceName);
                var dragDirection = (Vector3) _aeroDynamicForces.GetProgramVariable(currentDragDirectionName);
                var currentVelocity = (Vector3) _aeroDynamicForces.GetProgramVariable(currentVelocityName);
                var currentVelocityMagnitude = currentVelocity.magnitude;

                Gizmos.color = Color.white;
                GizmoUtils.DrawArrow(position, liftDirection, (float) liftForce);
                Handles.Label(position + (Vector3) liftDirection * (float) liftForce,
                    $" {(float) liftForce} N");

                Gizmos.color = Color.red;
                GizmoUtils.DrawArrow(position, (Vector3) dragDirection, (float) dragForce);
                Handles.Label(position + (Vector3) dragDirection * (float) dragForce,
                    $" {(float) dragForce} N");

                Gizmos.color = Color.cyan;
                GizmoUtils.DrawArrow(position, currentVelocity.normalized, currentVelocityMagnitude);
                Handles.Label(position + currentVelocity,
                    $"{currentVelocityMagnitude} m/s - {currentVelocityMagnitude * 3.6f} km/h");
            }
        }
    }
}
#endif
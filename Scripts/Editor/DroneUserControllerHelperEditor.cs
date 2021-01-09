using UnityEditor;
using UnityEngine;

namespace Guribo.FPVDrones.Scripts.Editor
{
    [CustomEditor(typeof(DroneUserControllerHelper))]
    public class DroneUserControllerHelperEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();
            var droneUserControllerHelper = (DroneUserControllerHelper) target;
            try
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Regenerate Drones"))
                {
                    droneUserControllerHelper.RegenerateDrones();
                }

                if (GUILayout.Button("Clear Drones"))
                {
                    droneUserControllerHelper.ClearDrones();
                }
            }
            finally
            {
                GUILayout.EndHorizontal();
            }
        }
    }
}
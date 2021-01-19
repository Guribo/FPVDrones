using UnityEditor;

namespace Guribo.FPVDrones.Scripts.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AeroDynamicForcesHelper))]
    public class AeroDynamicForcesHelperEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();
        }
    }
}
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Guribo.FPVDrones.Scripts.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(AeroDynamicForcesHelper))]
    public class AeroDynamicForcesHelperEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.DrawDefaultInspector();
            if (GUILayout.Button("Apply"))
            {
                var aeroDynamicForcesHelper = (AeroDynamicForcesHelper) target;
                if (aeroDynamicForcesHelper)
                {
                    aeroDynamicForcesHelper.Apply();
                }

                EditorSceneManager.MarkSceneDirty(aeroDynamicForcesHelper.gameObject.scene);
                EditorUtility.SetDirty(aeroDynamicForcesHelper.gameObject);
                EditorUtility.SetDirty(aeroDynamicForcesHelper);
            }
        }
    }
}
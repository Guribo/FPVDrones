#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Guribo.FPVDrones.Scripts
{
    public class DroneUserControllerHelper : MonoBehaviour
    {
        [SerializeField] protected GameObject dronePrefab;
        public string dronesVariableName = "drones";
        public int maxDrones = 80;

        [SerializeField] [HideInInspector] private List<GameObject> instantiatedDrones = new List<GameObject>();

        [ContextMenu("Clear Drones")]
        public void ClearDrones()
        {
            foreach (var instantiatedDrone in instantiatedDrones)
            {
                if (instantiatedDrone)
                {
                    DestroyImmediate(instantiatedDrone);
                }
            }

            instantiatedDrones.Clear();
        }

        [ContextMenu("Regenerate Drones")]
        public void GenerateDrones()
        {
            if (!dronePrefab)
            {
                Debug.LogError("No drone prefab specified", this);
                return;
            }

            ClearDrones();

            for (var i = 0; i < maxDrones; i++)
            {
                var connectedPrefabInstance =
                    PrefabUtility.InstantiatePrefab(dronePrefab, SceneManager.GetActiveScene());
                if (!connectedPrefabInstance)
                {
                    Debug.LogError("Failed to create connected prefab of the drone");
                    return;
                }

                var instantiate = (GameObject) connectedPrefabInstance;
                if (!instantiate)
                {
                    DestroyImmediate(connectedPrefabInstance);
                    return;
                }

                instantiate.name = $"GENERATED_{instantiate.name}_{i}";
                instantiatedDrones.Add(instantiate);
            }
        }
    }
}
#endif
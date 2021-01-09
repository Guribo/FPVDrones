#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Guribo.UdonUtils.Scripts;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.Udon;

namespace Guribo.FPVDrones.Scripts
{
    public class DroneUserControllerHelper : MonoBehaviour
    {
        [SerializeField] protected GameObject dronePrefab;
        public string dronesVariableName = "drones";
        public string betterAudioPoolVariableName = "betterAudioPool";
        public string customDroneInputVariableName = "customDroneInput";
        public int maxDrones = 80;
        public UdonBehaviour droneUserController;
        public UdonBehaviour spatializedAudioPool;
        public UdonBehaviour customDroneInput;

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
        public void RegenerateDrones()
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


            droneUserController.SetInspectorVariable(dronesVariableName, instantiatedDrones.ToArray());
            SetupDrones();
        }

        private void SetupDrones()
        {
            
            foreach (var instantiatedDrone in instantiatedDrones)
            {
                if (!instantiatedDrone) continue;
                foreach (var udonBehaviour in instantiatedDrone.GetComponentsInChildren<UdonBehaviour>())
                {
                    if (spatializedAudioPool)
                    {
                        try
                        {
                            if (udonBehaviour.GetInspectorVariableNames().Contains(betterAudioPoolVariableName))
                            {
                                udonBehaviour.SetInspectorVariable(betterAudioPoolVariableName,
                                    spatializedAudioPool);
                            }
                        }
                        catch (Exception e)
                        {
                            // ignored
                        }
                    }

                    if (customDroneInput)
                    {
                        try
                        {
                            if (udonBehaviour.GetInspectorVariableNames().Contains(customDroneInputVariableName))
                            {
                                udonBehaviour.SetInspectorVariable(customDroneInputVariableName,
                                    customDroneInput);
                            }
                        }
                        catch (Exception e)
                        {
                            // ignored
                        }
                    }
                }
            }
        }
    }
}
#endif
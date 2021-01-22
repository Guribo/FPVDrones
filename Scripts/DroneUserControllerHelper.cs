#if !COMPILER_UDONSHARP && UNITY_EDITOR
using System;
using System.Collections.Generic;
using Guribo.UdonUtils.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Udon;

namespace Guribo.FPVDrones.Scripts
{
    public class DroneUserControllerHelper : MonoBehaviour
    {
        [SerializeField] protected GameObject dronePrefab;
        public string dronesVariableName = "drones";
        public string dronesControllerVariableName = "droneControllers";
        public string betterAudioPoolVariableName = "betterAudioPool";
        public string customDroneInputVariableName = "customDroneInput";
        public int maxDrones = 80;
        public UdonBehaviour droneUserController;
        public UdonBehaviour spatializedAudioPool;
        public UdonBehaviour customDroneInput;

        public bool spawnAsPrefab;

        public string droneInputsName = "droneInputs";
        public List<UdonBehaviour> droneInputs;

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
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            EditorUtility.SetDirty(this.gameObject);
            EditorUtility.SetDirty(this);
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
                GameObject drone = null;
                if (spawnAsPrefab)
                {
                    var droneInstance = PrefabUtility.InstantiatePrefab(dronePrefab);
                    if (!droneInstance)
                    {
                        Debug.LogError("Failed to create connected prefab of the drone");
                        return;
                    }

                    drone = (GameObject) droneInstance;
                }
                else
                {
                    drone = Instantiate(dronePrefab);
                }

                if (!drone)
                {
                    Debug.LogError("Failed to create connected prefab of the drone");
                    return;
                }

                drone.name = $"GENERATED_{drone.name}_{i}";
                instantiatedDrones.Add(drone);
            }


            droneUserController.SetInspectorVariable(dronesVariableName, instantiatedDrones.ToArray());
            SetupDrones();
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
            EditorUtility.SetDirty(this.gameObject);
            EditorUtility.SetDirty(this);
        }

        private void SetupDrones()
        {
            var inputs = droneInputs.ToArray();
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

                            if (udonBehaviour.GetInspectorVariableNames().Contains(droneInputsName))
                            {
                                udonBehaviour.SetInspectorVariable(droneInputsName, inputs);
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
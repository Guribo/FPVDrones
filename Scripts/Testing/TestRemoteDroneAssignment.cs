using Guribo.UdonUtils.Scripts.Testing;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.FPVDrones.Scripts.Testing
{
    public class TestRemoteDroneAssignment : UdonSharpBehaviour
    {
        private const string LogPrefix = "[<color=#000000>FPVDrones</color>] [<color=#804500>Testing</color>]";

        #region DO NOT EDIT

        [HideInInspector] public TestController testController;

        public void Initialize()
        {
            if (!testController)
            {
                Debug.LogError(
                    $"{LogPrefix} {name}.Initialize: invalid test controller",
                    this);
                return;
            }

            Debug.Log($"{LogPrefix} {name}.Initialize", this);
            InitializeTest();
        }

        public void Run()
        {
            if (!testController)
            {
                Debug.LogError(
                    $"{LogPrefix} {name}.Run: invalid test controller",
                    this);
                return;
            }

            Debug.Log($"{LogPrefix} {name}.Run", this);
            RunTest();
        }

        public void CleanUp()
        {
            if (!testController)
            {
                Debug.LogError(
                    $"{LogPrefix} {name}.CleanUp: invalid test controller",
                    this);
                return;
            }

            Debug.Log($"{LogPrefix} {name}.CleanUp", this);
            CleanUpTest();
        }

        #endregion

        #region EDIT HERE

        public DroneUserController droneUserController;

        private bool _pendingCheck;
        private float _scheduledCheckTime;

        private void InitializeTest()
        {
            if (Networking.IsMaster)
            {
                Debug.Log($"{LogPrefix} {name}.InitializeTest: Master, nothing to do",
                    this);
                testController.TestInitialized(true);
                return;
            }

            if (!droneUserController)
            {
                Debug.LogError($"{LogPrefix} {name}.InitializeTest: invalid DroneUserController",
                    this);
                testController.TestInitialized(false);
                return;
            }

            var drones = droneUserController.drones;
            if (drones == null || drones.Length == 0)
            {
                Debug.LogError($"{LogPrefix} {name}.InitializeTest: no drones available",
                    droneUserController);
                testController.TestInitialized(false);
                return;
            }

            for (var i = 0; i < drones.Length; i++)
            {
                var drone = drones[i];
                if (!drone)
                {
                    Debug.LogError($"{LogPrefix} {name}.InitializeTest: invalid drone spawn at index {i}",
                        droneUserController);
                    testController.TestInitialized(false);
                    return;
                }
            }

            testController.TestInitialized(true);
        }

        private void Update()
        {
            if (!(_pendingCheck && Time.time > _scheduledCheckTime))
            {
                return;
            }

            _pendingCheck = false;

            var drones = droneUserController.drones;

            var vrcPlayerApi = Networking.LocalPlayer;
            if (vrcPlayerApi == null)
            {
                Debug.LogError($"{LogPrefix} {name}.RunTest: invalid local player",
                    this);
                testController.TestCompleted(false);
                return;
            }

            var masterPlayerId = vrcPlayerApi.playerId;
            var masterPilotedDrones = 0;

            for (var i = 0; i < drones.Length; i++)
            {
                var droneSpawn = drones[i];
                if (!droneSpawn)
                {
                    Debug.LogError($"{LogPrefix} {name}.RunTest: invalid drone spawn at index {i}",
                        droneUserController);
                    testController.TestCompleted(false);
                    return;
                }

                var droneGameObject = droneSpawn.transform.Find("Drone");
                if (!droneGameObject)
                {
                    Debug.LogError($"{LogPrefix} {name}.RunTest: invalid drone component at index {i}",
                        droneUserController);
                    testController.TestCompleted(false);
                    return;
                }

                var drone = droneGameObject.GetComponent<Drone>();
                if (!drone)
                {
                    Debug.LogError($"{LogPrefix} {name}.RunTest: invalid drone component at index {i}",
                        droneUserController);
                    testController.TestCompleted(false);
                    return;
                }

                if (drone.syncedPilotId == masterPlayerId)
                {
                    ++masterPilotedDrones;
                }
            }

            if (masterPilotedDrones != 1)
            {
                Debug.LogError($"{LogPrefix} {name}.RunTest: Non-Master is supposed to have exactly 1 drone " +
                               $"(has {masterPilotedDrones})",
                    this);
                testController.TestCompleted(false);
                return;
            }

            testController.TestCompleted(true);
        }

        private void RunTest()
        {
            if (Networking.IsMaster)
            {
                Debug.Log($"{LogPrefix} {name}.RunTest: Master, nothing to do",
                    this);
                testController.TestCompleted(true);
                return;
            }

            _scheduledCheckTime = Time.time
                                  + 2f // assumed network delay
                                  + 2f * droneUserController.updateCycleDuration // 2 update cycles
                                  + droneUserController.drones.Length *
                                  droneUserController
                                      .ownerTransitionWaitDuration; // assume each drone changes owner once

            _pendingCheck = true;

            Debug.Log(
                $"{LogPrefix} {name}.RunTest: Waiting for delayed checks",
                this);
        }

        private void CleanUpTest()
        {
            if (Networking.IsMaster)
            {
                Debug.Log($"{LogPrefix} {name}.CleanUpTest: Master, nothing to do",
                    this);
                testController.TestCleanedUp(true);
                return;
            }

            testController.TestCleanedUp(true);
        }

        #endregion
    }
}
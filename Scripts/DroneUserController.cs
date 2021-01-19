using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.FPVDrones.Scripts
{
    public class DroneUserController : UdonSharpBehaviour
    {
        private const int NoPilot = -1;
        public GameObject[] drones;
        public string droneChildName = "Drone";
        private int _updateIndex;
        private int _droneCount;
        private Drone[] _droneControllers;
        public int[] _dronePilots;
        private int _dronesInUse;
        private Drone _masterDrone;
        private VRCPlayerApi[] _players;

        public float ownerTransitionWaitDuration = 1f;
        public float updateCycleDuration = 10f;
        private float _updateInterval;
        private float _nextUpdateTime;

        private bool _initialized;

        #region Caching

        private VRCPlayerApi _localPlayer;
        private int _localPlayerId;

        #endregion

        #region Monobehaviour Methods

        private void Start()
        {
            if (!CacheLocalPlayer()
                || !InitDroneControllers()
                || !InitUpdateInterval())
            {
                return;
            }

            _initialized = true;
            Debug.Log("DroneUerController initialized successfully");
        }

        public void Update()
        {
            if (!_initialized)
            {
                return;
            }

            InputMasterResetDrones();

            if (IsWaitingForUpdate())
            {
                return;
            }

            if (Networking.IsMaster)
            {
                MasterUpdate();
            }
            else
            {
                RemotesUpdate();
            }
        }

        #endregion


        #region Init Methods

        private bool InitUpdateInterval()
        {
            if (drones == null || drones.Length == 0)
            {
                return false;
            }

            _updateInterval = updateCycleDuration / drones.Length;
            return true;
        }

        private bool CacheLocalPlayer()
        {
            _localPlayer = Networking.LocalPlayer;
            if (_localPlayer == null)
            {
                return false;
            }

            _localPlayerId = _localPlayer.playerId;
            return true;
        }

        private bool InitDroneControllers()
        {
            if (drones == null || drones.Length == 0)
            {
                return false;
            }

            _droneCount = drones.Length;
            _droneControllers = new Drone[_droneCount];
            _dronePilots = new int[_droneCount];

            for (var i = 0; i < drones.Length; i++)
            {
                if (!drones[i])
                {
                    Debug.LogError("Invalid drone in drones");
                    return false;
                }

                var droneChildGameObject = drones[i].transform.Find(droneChildName);
                if (!droneChildGameObject)
                {
                    Debug.LogError($"Child called {droneChildName} does not exist", drones[i]);
                    return false;
                }

                _droneControllers[i] = droneChildGameObject.GetComponent<Drone>();
                if (!_droneControllers[i])
                {
                    Debug.LogError($"Child called {droneChildName} is missing the Drone component",
                        droneChildGameObject);
                    return false;
                }

                _droneControllers[i].droneUserController = this;
            }

            return true;
        }

        #endregion

        #region Input

        private void InputMasterResetDrones()
        {
            if (Networking.IsMaster && Input.GetKeyDown(KeyCode.X))
            {
                _masterDrone = null;
                foreach (var droneController in _droneControllers)
                {
                    droneController.syncedPilotId = NoPilot;
                }

                InitDroneControllers();
            }
        }

        #endregion

        private bool IsWaitingForUpdate()
        {
            var unscaledTime = Time.unscaledTime;
            var canNotUpdate = unscaledTime < _nextUpdateTime;
            if (canNotUpdate) return true;
            _nextUpdateTime = unscaledTime + _updateInterval;
            return false;
        }


        public void MasterUpdate()
        {
            var drone = MasterGetNextDrone();
            if (!drone)
            {
                Debug.LogError($"Master: Drone {_updateIndex} is invalid");
                return;
            }

            var droneOwner = Networking.GetOwner(drone.gameObject);
            if (TryHandleDroneHasNoOwner(droneOwner, drone))
            {
                Debug.Log("Master: No drone owner found");
                return;
            }

            if (TryHandleOwnerIsPilot(droneOwner, drone))
            {
                Debug.Log("Master: owning player is also pilot");
                return;
            }

            if (TryHandleMasterOwnsDrone(droneOwner, drone))
            {
                return;
            }

            if (TryHandleDronePickedUp(drone))
            {
                Debug.Log("Master: drone is held by non-pilot user");
                return;
            }

            // once dropped return it to the pilot or master
            if (TryReturningControlToPilot(droneOwner, drone))
            {
                Debug.Log($"Master: returned drone {_updateIndex} control to pilot {droneOwner.playerId}");
                return;
            }

            if (TryLimitingToOnePilotPerDrone(droneOwner, drone))
            {
                return;
            }

            Debug.Log(drone.GetDebugText());
        }

        private bool TryLimitingToOnePilotPerDrone(VRCPlayerApi droneOwner, Drone drone)
        {
            var droneOwnerShipCount = 0;
            var droneOwnerPlayerId = droneOwner.playerId;
            foreach (var dronePilot in _dronePilots)
            {
                if (dronePilot == droneOwnerPlayerId)
                {
                    ++droneOwnerShipCount;
                }
            }

            if (droneOwnerShipCount > 1)
            {
                // make the master (this user) the owner
                Debug.Log($"Master: remove drone {_updateIndex} from owner {droneOwnerPlayerId}");
                Networking.SetOwner(_localPlayer, drone.gameObject);
                drone.syncedPilotId = NoPilot;
                _dronePilots[_updateIndex] = NoPilot;
                drone.OnOwnershipTransferred();
                _nextUpdateTime += ownerTransitionWaitDuration;
                return true;
            }

            if (droneOwnerShipCount == 0)
            {
                Debug.Log($"Master: giving free drone {_updateIndex} to owner {droneOwnerPlayerId}");
                // make the current owner the pilot
                _dronePilots[_updateIndex] = droneOwnerPlayerId;
                Networking.SetOwner(droneOwner, drone.gameObject);
                drone.syncedPilotId = droneOwnerPlayerId;
                drone.OnOwnershipTransferred();
                _nextUpdateTime += ownerTransitionWaitDuration;
            }

            return false;
        }

        private bool TryReturningControlToPilot(VRCPlayerApi droneOwner, Drone drone)
        {
            var currentPilot = VRCPlayerApi.GetPlayerById(_dronePilots[_updateIndex]);
            if (currentPilot != null
                && currentPilot.playerId != droneOwner.playerId
                && drone.syncedPilotId != currentPilot.playerId)
            {
                // return drone ownership to the former pilot
                Networking.SetOwner(currentPilot, drone.gameObject);
                drone.syncedPilotId = currentPilot.playerId;
                drone.OnOwnershipTransferred();
                _nextUpdateTime += ownerTransitionWaitDuration;
                return true;
            }

            return false;
        }

        private bool TryHandleDronePickedUp(Drone drone)
        {
            var pickup = drone.vrcPickup;
            if (pickup.IsHeld || pickup.currentPlayer != null)
            {
                // let the player hold it
                return true;
            }

            return false;
        }

        private bool TryHandleOwnerIsPilot(VRCPlayerApi droneOwner, Drone drone)
        {
            var ownerIsPilot = droneOwner.playerId == _dronePilots[_updateIndex]
                               && droneOwner.playerId == drone.syncedPilotId;
            if (ownerIsPilot)
            {
                // nothing to do
                return true;
            }

            return false;
        }

        private bool TryHandleMasterOwnsDrone(VRCPlayerApi droneOwner, Drone drone)
        {
            var masterOwnsDrone = droneOwner.playerId == _localPlayerId;
            if (!masterOwnsDrone)
            {
                Debug.Log($"Master: does not own drone {_updateIndex}");
                return false;
            }

            Debug.Log("masterOwnsDrone");

            if (TryHandleMasterControlsNoDrone(drone))
            {
                Debug.Log($"Master: started controlling drone {_updateIndex}");
                return true;
            }

            var isMasterControlled = _masterDrone == drone;
            if (isMasterControlled)
            {
                Debug.Log($"Master: still controls drone {_updateIndex}");
                drone.syncedPilotId = _localPlayerId;
                return true;
            }

            if (HandleDroneAvailable(drone))
            {
                Debug.Log($"Master: found a new pilot {_dronePilots[_updateIndex]} for drone {_updateIndex}");
                return true;
            }

            return false;
        }

        private bool HandleDroneAvailable(Drone drone)
        {
            var freePilotSlot = FindFreePilotSlot();
            // abort if no free slot is available
            if (freePilotSlot == -1)
            {
                return false;
            }

            var playerCount = UpdatePlayerList();
            for (var i = 0; i < playerCount; i++)
            {
                var player = _players[i];
                if (player == null)
                {
                    continue;
                }

                var playerId = player.playerId;
                var isPilot = PlayerIsPilot(playerId);
                if (isPilot)
                {
                    // keep searching while the players are pilots
                    continue;
                }

                // player found that is not a pilot, give the player the drone
                Networking.SetOwner(player, drone.gameObject);
                drone.syncedPilotId = playerId;
                _dronePilots[_updateIndex] = playerId;
                drone.OnOwnershipTransferred();
                _nextUpdateTime += ownerTransitionWaitDuration;
                return true;
            }

            return false;
        }

        private bool TryHandleMasterControlsNoDrone(Drone drone)
        {
            if (_masterDrone)
            {
                return false;
            }

            _masterDrone = drone;
            Networking.SetOwner(_localPlayer, drone.gameObject);
            drone.syncedPilotId = _localPlayerId;
            _dronePilots[_updateIndex] = _localPlayerId;
            _nextUpdateTime += ownerTransitionWaitDuration;
            drone.OnOwnershipTransferred();
            return true;
        }

        private bool TryHandleDroneHasNoOwner(VRCPlayerApi droneOwner, Drone drone)
        {
            if (droneOwner == null)
            {
                Networking.SetOwner(_localPlayer, drone.gameObject);
                drone.syncedPilotId = NoPilot;
                _dronePilots[_updateIndex] = NoPilot;
                _nextUpdateTime += ownerTransitionWaitDuration;
                drone.OnOwnershipTransferred();
                // leave for now and try again later to make sure the ownership is completely transferred
                return true;
            }

            return false;
        }

        private Drone MasterGetNextDrone()
        {
            if (MasterEnsureOwnership())
            {
                return null;
            }

            if (_droneCount == 0)
            {
                Debug.LogError("No drones");
                return null;
            }

            _updateIndex = (_updateIndex + 1) % _droneCount;
            if (InvalidIndex(_updateIndex))
            {
                return null;
            }

            Debug.Log($"Master checking drone {_updateIndex}");

            return _droneControllers[_updateIndex];
        }

        private bool MasterEnsureOwnership()
        {
            if (!Networking.IsOwner(_localPlayer, gameObject))
            {
                Networking.SetOwner(_localPlayer, gameObject);
                _nextUpdateTime += ownerTransitionWaitDuration;
                return true;
            }

            return false;
        }

        private bool InvalidIndex(int updateIndex)
        {
            if (_droneControllers == null || updateIndex < 0 || updateIndex >= _droneControllers.Length)
            {
                Debug.LogError("Invalid _droneControllers");
                return true;
            }


            if (_dronePilots != null && updateIndex < _dronePilots.Length)
            {
                return false;
            }

            Debug.LogError("Invalid _dronePilots");
            return true;
        }


        private void SpawnDroneForPlayer(Drone drone, VRCPlayerApi player)
        {
            var playerRotation = player.GetRotation();
            drone.gameObject.transform.SetPositionAndRotation(
                player.GetPosition() + playerRotation * Vector3.forward, playerRotation);
        }

        private bool PlayerIsPilot(int playerId)
        {
            var isPilot = false;
            foreach (var dronePilot in _dronePilots)
            {
                if (dronePilot != playerId)
                {
                    continue;
                }

                isPilot = true;
                break;
            }

            return isPilot;
        }

        private int FindFreePilotSlot()
        {
            // find a free pilot slot
            var freePilotSlot = -1;

            for (var i = 0; i < _dronePilots.Length; i++)
            {
                var dronePilotId = _dronePilots[i];
                var pilotSlotIsUsed = dronePilotId != NoPilot && VRCPlayerApi.GetPlayerById(dronePilotId) != null;
                if (pilotSlotIsUsed)
                {
                    continue;
                }

                freePilotSlot = i;
                break;
            }

            return freePilotSlot;
        }

        private int UpdatePlayerList()
        {
            var playerCount = VRCPlayerApi.GetPlayerCount();
            if (_players == null || _players.Length < playerCount)
            {
                _players = new VRCPlayerApi[playerCount];
            }

            _players = VRCPlayerApi.GetPlayers(_players);
            return playerCount;
        }

        public void RemotesUpdate()
        {
            var drone = RemotesGetNextDrone();
            if (!drone)
            {
                return;
            }

            _dronePilots[_updateIndex] = drone.syncedPilotId;
        }

        private Drone RemotesGetNextDrone()
        {
            _updateIndex = (_updateIndex + 1) % _droneCount;
            if (InvalidIndex(_updateIndex))
            {
                return null;
            }

            return _droneControllers[_updateIndex];
        }


        public void SetCustomInputChanged()
        {
            if (_droneControllers == null)
            {
                return;
            }

            foreach (var drone in _droneControllers)
            {
                if (!drone) continue;
                drone.customDroneInputChanged = true;
            }
        }
    }
}
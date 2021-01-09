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
        private int _droneCount = 0;
        private Drone[] _droneControllers;
        private int[] _dronePilots;
        private int _dronesInUse;
        private Drone _masterDrone;
        private VRCPlayerApi[] _players;

        public float ownerTransitionWaitDuration = 1f;
        public float updateCycleDuration = 10f;
        private float _updateInterval;
        private float _nextUpdateTime;


        private void Start()
        {
            _updateInterval = updateCycleDuration / Mathf.Max(drones.Length, 1f);
            InitDroneControllers();
        }

        public void Update()
        {
            MasterResetDrones();

            if (WaitingForUpdate())
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

        private bool WaitingForUpdate()
        {
            var unscaledTime = Time.unscaledTime;
            var canNotUpdate = unscaledTime < _nextUpdateTime;
            if (canNotUpdate) return true;
            _nextUpdateTime = unscaledTime + _updateInterval;
            return false;
        }

        private void MasterResetDrones()
        {
            if (Networking.IsMaster && Input.GetKeyDown(KeyCode.X))
            {
                InitDroneControllers();
            }
        }

        // public override void OnOwnershipTransferred()
        // {
        // }
        //
        // public override void OnPlayerJoined(VRCPlayerApi player)
        // {
        // }
        //
        // public override void OnPlayerLeft(VRCPlayerApi player)
        // {
        // }

        public void MasterUpdate()
        {
            var localPlayer = Networking.LocalPlayer;

            // Ensure the master only manages drone pilots when he is also owner
            if (!Networking.IsOwner(localPlayer, gameObject))
            {
                Networking.SetOwner(localPlayer, gameObject);
                _nextUpdateTime += ownerTransitionWaitDuration;
                return;
            }

            if (_droneCount == 0)
            {
                Debug.LogError("No drones");
                return;
            }

            _updateIndex = (_updateIndex + 1) % _droneCount;
            if (InvalidIndex(_updateIndex))
            {
                return;
            }

            Debug.Log($"Master checking drone {_updateIndex}");

            var drone = _droneControllers[_updateIndex];
            if (!drone) return;

            var droneOwner = Networking.GetOwner(drone.gameObject);
            var droneIsNotYetOwnedByMaster = droneOwner == null;
            if (droneIsNotYetOwnedByMaster)
            {
                Debug.Log("droneIsNotYetOwnedByMaster");
                Networking.SetOwner(localPlayer, drone.gameObject);
                _nextUpdateTime += ownerTransitionWaitDuration;
                // leave for now and try again later to make sure the ownership is completely transferred
                return;
            }

            var masterOwnsDrone = droneOwner.playerId == localPlayer.playerId &&
                                  droneOwner.playerId == drone.syncedPilotId;
            if (masterOwnsDrone)
            {
                Debug.Log("masterOwnsDrone");
                HandleMasterOwnsDrone(drone, _updateIndex, localPlayer.playerId);
                return;
            }

            // master does not own drone,
            // it is either owned by it's pilot
            // or by another user that maybe grabbed it

            var ownerIsPilot = droneOwner.playerId == _dronePilots[_updateIndex]
                               && droneOwner.playerId == drone.syncedPilotId;
            if (ownerIsPilot)
            {
                Debug.Log("ownerIsPilot");
                // nothing to do
                return;
            }

            // owner is not pilot, return the drone to the pilot once dropped etc.
            var pickup = drone.vrcPickup;
            if (pickup.IsHeld || pickup.currentPlayer != null)
            {
                Debug.Log("drone is held by non-pilot user");
                // let the player hold it
                return;
            }

            Debug.Log("drone is no longer held by non-pilot user");
            // once dropped return it to the pilot or master
            var pilot = VRCPlayerApi.GetPlayerById(_dronePilots[_updateIndex]);
            if (pilot != null)
            {
                // return drone ownership to the former pilot
                Networking.SetOwner(pilot, drone.gameObject);
                drone.syncedPilotId = pilot.playerId;
                _nextUpdateTime += ownerTransitionWaitDuration;
                return;
            }

            // no pilot exists
            var currentOwnerAlreadyHasADrone = false;
            foreach (var dronePilot in _dronePilots)
            {
                if (dronePilot == droneOwner.playerId)
                {
                    currentOwnerAlreadyHasADrone = true;
                    break;
                }
            }

            if (currentOwnerAlreadyHasADrone)
            {
                // make the master (this user) the owner
                _dronePilots[_updateIndex] = NoPilot;
                _nextUpdateTime += ownerTransitionWaitDuration;
                return;
            }

            // make the current owner the pilot
            _dronePilots[_updateIndex] = droneOwner.playerId;
            Networking.SetOwner(droneOwner, drone.gameObject);
            drone.syncedPilotId = droneOwner.playerId;
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


        private void HandleMasterOwnsDrone(Drone drone, int droneIndex, int masterPlayerId)
        {
            if (_masterDrone == null)
            {
                _masterDrone = drone;
                Networking.SetOwner(Networking.LocalPlayer, drone.gameObject);
                drone.syncedPilotId = masterPlayerId;
                _dronePilots[droneIndex] = masterPlayerId;
            }
            else
            {
                if (_masterDrone != drone)
                {
                    // drone is unused and thus free
                    // a new user could take it


                    var freePilotSlot = FindFreePilotSlot();

                    // abort if no free slot is available
                    if (freePilotSlot == -1)
                    {
                        return;
                    }

                    // find a player that has no drone yet
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

                        // keep searching while the players are pilots
                        if (isPilot)
                        {
                            continue;
                        }

                        // give the player a drone and leave the loop
                        Networking.SetOwner(player, drone.gameObject);
                        drone.syncedPilotId = playerId;
                        _dronePilots[freePilotSlot] = playerId;
                        // drone.pilotId = playerId;

                        SpawnDroneForPlayer(drone, player);
                        break;
                    }
                }
                else
                {
                    Debug.Log("Master has a drone");
                    drone.syncedPilotId = masterPlayerId;
                }
            }
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
                if (dronePilot == playerId)
                {
                    isPilot = true;
                    break;
                }
            }

            return isPilot;
        }

        private int FindFreePilotSlot()
        {
            // find a free pilot slot
            var freePilotSlot = -1;

            for (var i = 0; i < _dronePilots.Length; i++)
            {
                if (_dronePilots[i] == NoPilot || VRCPlayerApi.GetPlayerById(_dronePilots[i]) != null)
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
            var localPlayer = Networking.LocalPlayer;
            if (localPlayer == null)
            {
                return;
            }

            if (_droneCount == 0)
            {
                return;
            }

            _updateIndex = (_updateIndex + 1) % _droneCount;
            if (_droneControllers == null || _updateIndex < 0 || _updateIndex >= _droneControllers.Length)
            {
                return;
            }

            if (_dronePilots == null || _updateIndex < 0 || _updateIndex >= _dronePilots.Length)
            {
                return;
            }

            Debug.Log($"Remote checking drone {_updateIndex}");

            var drone = _droneControllers[_updateIndex];
            if (!drone) return;

            var droneOwner = Networking.GetOwner(drone.gameObject);
            var noOwnerSetYet = droneOwner == null;
            if (noOwnerSetYet)
            {
                _dronePilots[_updateIndex] = NoPilot;
                return;
            }

            // owner is not pilot, return the drone to the pilot once dropped etc.
            var pickup = drone.vrcPickup;
            if (pickup && pickup.IsHeld)
            {
                // let the player hold it
                return;
            }

            if (droneOwner.playerId == localPlayer.playerId)
            {
                _dronePilots[_updateIndex] = localPlayer.playerId;
                drone.syncedPilotId = localPlayer.playerId;
                return;
            }

            drone.syncedPilotId = NoPilot;
            _dronePilots[_updateIndex] = droneOwner.playerId;
        }

        private void InitDroneControllers()
        {
            if (drones == null)
            {
                return;
            }

            _droneCount = drones.Length;
            _droneControllers = new Drone[_droneCount];
            _dronePilots = new int[_droneCount];

            for (var i = 0; i < drones.Length; i++)
            {
                if (!drones[i])
                {
                    Debug.LogError("Invalid drone in drones");
                    return;
                }

                var droneChildGameObject = drones[i].transform.Find(droneChildName);
                if (!droneChildGameObject)
                {
                    Debug.LogError($"Child called {droneChildName} does not exist", drones[i]);
                    return;
                }

                _droneControllers[i] = droneChildGameObject.GetComponent<Drone>();
                if (!_droneControllers[i])
                {
                    Debug.LogError($"Child called {droneChildName} is missing the Drone component",
                        droneChildGameObject);
                    return;
                }

                _droneControllers[i].droneUserController = this;
            }
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
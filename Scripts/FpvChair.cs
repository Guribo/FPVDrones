using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Guribo.FPVDrone.Scripts
{
    public class FpvChair : UdonSharpBehaviour
    {
        [SerializeField] private Drone drone;
        [SerializeField] private VRCStation station;


        public override void Interact()
        {
            station.seated = true;
            station.disableStationExit = true;
            station.PlayerMobility = VRCStation.Mobility.ImmobilizeForVehicle;

            var localPlayer = Networking.LocalPlayer;
            if (localPlayer == null)
            {
                return;
            }

            if (!drone)
            {
                return;
            }

            localPlayer.UseAttachedStation();
            Networking.SetOwner(localPlayer, drone.gameObject);
            drone.pilotId = localPlayer.playerId;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                var localPlayer = Networking.LocalPlayer;
                if (localPlayer == null)
                {
                    return;
                }

                station.ExitStation(localPlayer);
            }
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (player == null)
            {
                return;
            }

            var localPlayer = Networking.LocalPlayer;
            if (localPlayer == null)
            {
                return;
            }

            if (localPlayer.playerId == player.playerId)
            {
                localPlayer.Immobilize(true);
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            StopPilotingDrone();
        }

        private void StopPilotingDrone()
        {
            if (!drone)
            {
                return;
            }

            if (drone.pilotId == drone.noPilot)
            {
                return;
            }

            drone.pilotId = -1;
        }

        public void OnDisable()
        {
            StopPilotingDrone();
        }

        public void OnDestroy()
        {
            StopPilotingDrone();
        }
    }
}
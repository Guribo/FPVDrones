
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WayPointNetwork : UdonSharpBehaviour
{

    public WayPoint[] wayPoints;
    public float maxNeighbourDistance = 10f;
    
    void Start()
    {
        if (wayPoints == null || wayPoints.Length == 0)
        {
            Debug.LogError("No waypoints specified");
            return;
        }

        foreach (var wayPoint in wayPoints)
        {
            if (!wayPoint)
            {
                Debug.LogWarning("Empty entry in wayPoints");
                continue;
            }

            wayPoint.Initialize(wayPoints, maxNeighbourDistance);
        }
    }
}

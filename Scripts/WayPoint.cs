
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class WayPoint : UdonSharpBehaviour
{

    private WayPoint[] _neighbourWayPoints;
    public void Initialize(WayPoint[] wayPoints, float maxNeighbourDistance)
    {
        if (wayPoints == null || wayPoints.Length == 0)
        {
            Debug.LogError("No waypoints received");
            return;
        }

        if (maxNeighbourDistance <= 0f)
        {
            Debug.LogError($"maxNeighbourDistance distance ({maxNeighbourDistance}) is useless");
            return;
        }

        // create a temporary over-sized array to not have to resize it every time we add a waypoint
        var myNeighbors = new WayPoint[wayPoints.Length];
        int wayPointsAdded = 0;
        
        foreach (var wayPoint in wayPoints)
        {
            // skip empty waypoints
            if (!wayPoint) continue;
            
            // ignore waypoint if it is this waypoint
            if(wayPoint == this) continue;

            // if the waypoint is within the neighbour range add it to our list
            if (Vector3.Distance(transform.position, wayPoint.transform.position) < maxNeighbourDistance)
            {
                myNeighbors[wayPointsAdded] = wayPoint;
                // increment the index
                ++wayPointsAdded;
            }
        }

        // log a warning 
        if (wayPointsAdded == 0)
        {
            Debug.LogWarning($"No waypoints added to {gameObject.name}", this);
        }
        
        // create the array that is just as big as we need it to be (can be empty! aka. Length == 0)
        _neighbourWayPoints = new WayPoint[wayPointsAdded];
        
        // copy the waypoints to the new array
        for (int i = 0; i < wayPointsAdded; i++)
        {
            _neighbourWayPoints[i] = myNeighbors[i];
        }
    }
}

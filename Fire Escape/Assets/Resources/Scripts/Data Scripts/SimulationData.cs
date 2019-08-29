using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SimulationData
{
    public EvacueeData[] evacData;
    public FireExitData[] fireExitData;
    public WindowData[] winData;
    public WallData[] wallData;
    
    public SimulationData(SimulationObjectLists simObjLists)
    {
        evacData = new EvacueeData[simObjLists.getEvacueeList().Count];
        fireExitData = new FireExitData[simObjLists.getFireExitList().Count];
        winData = new WindowData[simObjLists.getWindowList().Count];
        wallData = new WallData[simObjLists.getWallList().Count];
        int count = 0;
        foreach(GameObject evacuee in simObjLists.getEvacueeList())
        {
            evacData[count] = new EvacueeData(evacuee);
            count++;
        }
        count = 0;
        foreach(GameObject fireExit in simObjLists.getFireExitList())
        {
            fireExitData[count] = new FireExitData(fireExit);
            count++;
        }
        count = 0;
        foreach(GameObject window in simObjLists.getWindowList())
        {
            winData[count] = new WindowData(window);
            count++;
        }
        count = 0;
        foreach(GameObject wall in simObjLists.getWallList())
        {
            wallData[count] = new WallData(wall);
            count++;
        }
    }
}

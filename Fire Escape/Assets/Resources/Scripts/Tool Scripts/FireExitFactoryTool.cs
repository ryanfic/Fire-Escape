using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireExitFactoryTool : EvacuationObjectFactoryTool
{
    public override string Name {get {return "FireExitFactory";}}
    
    void Awake()
    {
        base.Awake();
        //arrowPrefab = (GameObject)Resources.Load("Prefabs/Arrow", typeof(GameObject));
        evacuationObjectPrefab = (GameObject)Resources.Load("Prefabs/FireExit", typeof(GameObject));
        evacuationObjectHeight = 1f;
        
    }

    protected override void setObjectInfo()
    {
        newEvacuationObject.name = "Fire Exit";
        newEvacuationObject.tag = "Fire Exit";
        newEvacuationObject.layer = LayerMask.NameToLayer("FireExit");
        //add the fire exit to the UI controller's Fire exit list
        objLists.addFireExit(newEvacuationObject);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvacueeFactoryTool : EvacuationObjectFactoryTool
{
    public override string Name {get {return "EvacueeFactory";}}
    
    void Awake()
    {
        base.Awake();
        //arrowPrefab = (GameObject)Resources.Load("Prefabs/Arrow", typeof(GameObject));
        evacuationObjectPrefab = (GameObject)Resources.Load("Prefabs/Evacuee", typeof(GameObject));
        evacuationObjectHeight = 1f;
        
    }
    protected override void setObjectInfo()
    {
        newEvacuationObject.name = "Evacuee";
        newEvacuationObject.tag = "Evacuee";
        newEvacuationObject.layer = LayerMask.NameToLayer("Evacuee");
        //add the evacuee to the UI controller's evacuee list
        objLists.addEvacuee(newEvacuationObject);
    }
}

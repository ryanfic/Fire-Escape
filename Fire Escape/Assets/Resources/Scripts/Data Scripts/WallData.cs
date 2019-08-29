using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WallData : EgressObjectData
{
    public float[] scale;
   public WallData(GameObject wall):base(wall,"Wall")
   {
       scale = new float[3];
       if(wall.layer == LayerMask.NameToLayer(objType))
        {
            //set up the data
            scale[0] = wall.transform.localScale.x;
            scale[1] = wall.transform.localScale.y;
            scale[2] = wall.transform.localScale.z;
        }
        else
        {
            scale[0] = 1f;
            scale[1] = 1f;
            scale[2] = 1f;
        }
   }
}

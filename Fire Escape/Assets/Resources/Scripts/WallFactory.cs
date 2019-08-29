using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallFactory : MonoBehaviour
{

    public GameObject wallPrefab;
    public float wallWidth;
    public float wallHeight;
    public static GameObject staticWallPrefab;
    public static float staticWallWidth;
    public static float staticWallHeight;
    //Private Constructor.  
    private WallFactory()  
    {  
    }  
    /*private WallFactory(GameObject _wallPrefab, float _wallWidth, float _wallHeight)  
    {  
        wallPrefab = _wallPrefab;
        wallWidth = _wallWidth;
        wallHeight = _wallHeight;
    }  */
    void Awake(){
        staticWallPrefab = wallPrefab;
        staticWallWidth = wallWidth;
        staticWallHeight = wallHeight;
    }
    private static WallFactory instance = null;  
    public static WallFactory Instance  
    {  
        get  
        {  
            if (instance == null)  
            {  
                instance = new WallFactory();
            }  
            return instance;  
        }  
    }  
    public GameObject createWall(float length, Vector3 position, Quaternion rotation){
        //create the wall
        GameObject wall = Instantiate(staticWallPrefab,position,rotation);
        //change the length to the length value
        wall.transform.localScale = new Vector3(staticWallWidth,staticWallHeight,length);
        //tag the wall
        wall.tag = "Wall";
        //name the wall
        wall.name = "Wall";
        wall.layer = LayerMask.NameToLayer("Wall");
        //Debug.Log("Created Wall!");
        //Debug.Log("Height: " + staticWallHeight);
        return wall;
    }

}

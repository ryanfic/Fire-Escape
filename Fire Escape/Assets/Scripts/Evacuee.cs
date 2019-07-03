using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Evacuee : MonoBehaviour
{   

    public float turnRadius = 10f; //The distance that the agent will think it is too close to a wall, and will attempt to find another spot to move to

    public float wanderTime = 0f; //The time since the last Wander function has been called
    public float wanderInterval = 3f; //The interval between Wander() calls

    public GameObject searchedZonePrefab;

    public FieldOfView fov;


    public enum EvacueeMovementState{
        toExit,toStairs,wandering
    }
    public EvacueeMovementState curMoveState = EvacueeMovementState.wandering;

    




    private List<GameObject> seenExits = new List<GameObject>();
    private List<GameObject> seenStairs = new List<GameObject>();

    void Start(){
        wanderTime = wanderInterval;
        fov = gameObject.GetComponent<FieldOfView>();
    }

    void Update(){
        //clear seenExits
        seenExits.Clear();

        //if the agent sees any exits
        foreach(Transform exit in fov.visibleTargets)
        {
            //check if it is already seen
            
            //Debug.Log("Saw " + exit);
            //add the exit to seenExits
            seenExits.Add(exit.gameObject);
        }
        //if the agent sees stairs, add it to the seenStairs
        

        //if the agent sees a new stair, use it

        //if the time since last Wander call is greater than or equal to the Wander call interval
        if(wanderTime>=wanderInterval)
        {
            //reset wander time
            wanderTime = 0;
            //call Wander
            Wander();
        }
        else
        {
            wanderTime += Time.deltaTime;
        }
    }

    private void Wander(){


        //
        
        
    }

    /*
        
     */
    private void pathfind(Vector3 direction){
        
    }
}

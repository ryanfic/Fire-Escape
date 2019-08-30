using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.Linq;
using UnityEngine.UI;

public class UiController : MonoBehaviour
{
    public Camera cam;
    public GameObject[] editorButtons;
    public GameObject[] runtimeButtons;
    private Dictionary<string,Tool> toolsByName; //dictionary (AKA Map) of all tools
    private SimulationObjectLists objLists;
    private SimulationData playData;
    private string selectedTool; //the selected tool
    

    void Awake()
    {
        //create a new simulation object list
        objLists = gameObject.AddComponent<SimulationObjectLists>() as SimulationObjectLists;
        //get all types of tools that are not abstract
        var toolTypes = Assembly.GetAssembly(typeof(Tool)).GetTypes()
        .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Tool)));

        toolsByName = new Dictionary<string,Tool>();

        //put tools into the dictionary
        foreach(var type in toolTypes)
        {
            //create a tool script reference
            Tool tempTool = gameObject.AddComponent(type) as Tool;

            //start the tool as disabled
            tempTool.enabled = false;

            //make the tool have the correct camera
            tempTool.switchCamera(cam);
            
            //let the tool have a reference to the UIController
            tempTool.addSimObjLists(objLists);
            
            //add the tool to the dictionary
            toolsByName.Add(tempTool.Name,tempTool);
        }
        selectedTool = "";
        //Debug.Log("Type: " + LayerMask.LayerToName(_objType.value));
        /* try{
            ((WallBuilderTool)toolsByName["WallBuilder"]).switchCamera(cam);
        }
        catch (KeyNotFoundException){
            Debug.Log("Wall Builder not found.");
        }
        try{
            ((EvacueeFactoryTool)toolsByName["EvacueeFactory"]).switchCamera(cam);
        }
        catch (KeyNotFoundException){
            Debug.Log("Evacuee Factory not found.");
        }
        try{
            ((FireExitFactoryTool)toolsByName["FireExitFactory"]).switchCamera(cam);
        }
        catch (KeyNotFoundException){
            Debug.Log("Fire Exit Factory not found.");
        }
        try{
            ((WindowFactoryTool)toolsByName["WindowFactory"]).switchCamera(cam);
        }
        catch (KeyNotFoundException){
            Debug.Log("Window Factory not found.");
        }*/

        /* foreach(KeyValuePair<string,Tool> entry in toolsByName)
        {
            
            Debug.Log("Tool: " + entry.Value.Name);
        }*/
    }

    
    public void switchToEvacueeFactoryTool()
    {
        if(selectedTool != "EvacueeFactory")
        {
            disableTool(selectedTool);
            enableTool("EvacueeFactory");
        }
    }
    public void switchToFireExitFactoryTool()
    {
        if(selectedTool != "FireExitFactory")
        {
            disableTool(selectedTool);
            enableTool("FireExitFactory");
        }
    }
    public void switchToWindowFactoryTool()
    {
        if(selectedTool != "WindowFactory")
        {
            disableTool(selectedTool);
            enableTool("WindowFactory");
        }
    }
    public void switchToWallBuilderTool()
    {
        if(selectedTool != "WallBuilder")
        {
            disableTool(selectedTool);
            enableTool("WallBuilder");
        }
    }
    public void switchToRemovalTool()
    {
        if(selectedTool != "Removal")
        {
            disableTool(selectedTool);
            enableTool("Removal");
        }
    }
    private void enableTool(string toolName)
    {
        //if the dictionary of tools has the tool
        if(toolsByName.ContainsKey(toolName))
        {
            //enable the tool
            toolsByName[toolName].enabled = true;
            //set the current tool to the tool now enabled
            selectedTool = toolName;
        }
    }
    private void disableTool(string toolName)
    {
        //if the dictionary of tools has the tool
        if(toolsByName.ContainsKey(toolName))
        {
            //disable the tool
            toolsByName[toolName].enabled = false;
        }
    }
    public void runSimulation()
    {
        Debug.Log("Running sim");
        //save simulation data to playData
        playData = new SimulationData(objLists);
        //enable the evacuee script/vision cones
        foreach(GameObject evacuee in objLists.getEvacueeList())
        {
            evacuee.GetComponent<FieldOfView>().enabled = true;
            Evacuee script = evacuee.GetComponent<Evacuee>();
            script.enabled = true;
            //add simulationobjectlists to evacuee
            script.addSimObjList(objLists);

            
        }
        Debug.Log("finished turning on evacuees");
        //switch off all editor buttons
        foreach(GameObject btn in editorButtons)
        {
            btn.SetActive(false);
        }
        Debug.Log("Finished turning off buttons");
        //switch on all runtime buttons
        foreach(GameObject btn in runtimeButtons)
        {
            btn.SetActive(true);
        }
        Debug.Log("Finished turning ON butns");
    }
    public void endSimulation()
    {
        //delete everything
        foreach(GameObject evac in objLists.getEvacueeList())
        {
            Destroy(evac);
        }
        objLists.clearEvacueeList();
        foreach(GameObject fe in objLists.getFireExitList())
        {
            Destroy(fe);
        }
        objLists.clearFireExitList();
        foreach(GameObject window in objLists.getWindowList())
        {
            Destroy(window);
        }
        objLists.clearWindowList();
        foreach(GameObject wall in objLists.getWallList())
        {
            Destroy(wall);
        }
        objLists.clearWallList();
        foreach(GameObject footprint in objLists.getFootprintList())
        {
            Destroy(footprint);
        }
        objLists.clearFootprintList();
        //respawn everything using playData
        loadSimulation(playData);
        //switch on all editor buttons
        foreach(GameObject btn in editorButtons)
        {
            btn.SetActive(true);
        }
        //switch off all runtime buttons
        foreach(GameObject btn in runtimeButtons)
        {
            btn.SetActive(false);
        }
    }
    
    public void saveSimulation()
    {
        disableTool(selectedTool);
        SaveSystem.SaveSimData(objLists);
        
    }
    public void loadSimulation()
    {
        disableTool(selectedTool);
        SimulationData simData = SaveSystem.LoadSimData();
        foreach(EvacueeData evacueeData in simData.evacData)
        {
            Vector3 evacPos = new Vector3(evacueeData.position[0],evacueeData.position[1],evacueeData.position[2]);
            Quaternion evacRot = new Quaternion(evacueeData.rotation[0],evacueeData.rotation[1],evacueeData.rotation[2],evacueeData.rotation[3]);
            ((EvacueeFactoryTool)toolsByName["EvacueeFactory"]).createObject(evacPos,evacRot);
        }
        foreach(FireExitData fireExitData in simData.fireExitData)
        {
            Vector3 fePos = new Vector3(fireExitData.position[0],fireExitData.position[1],fireExitData.position[2]);
            Quaternion feRot = new Quaternion(fireExitData.rotation[0],fireExitData.rotation[1],fireExitData.rotation[2],fireExitData.rotation[3]);
            ((FireExitFactoryTool)toolsByName["FireExitFactory"]).createObject(fePos,feRot);
        }
        foreach(WindowData winData in simData.winData)
        {
            Vector3 winPos = new Vector3(winData.position[0],winData.position[1],winData.position[2]);
            Quaternion winRot = new Quaternion(winData.rotation[0],winData.rotation[1],winData.rotation[2],winData.rotation[3]);
            ((WindowFactoryTool)toolsByName["WindowFactory"]).createObject(winPos,winRot);
        }
        foreach(WallData wallData in simData.wallData)
        {
            Vector3 wallPos = new Vector3(wallData.position[0],wallData.position[1],wallData.position[2]);
            Quaternion wallRot = new Quaternion(wallData.rotation[0],wallData.rotation[1],wallData.rotation[2],wallData.rotation[3]);
            Vector3 wallScale = new Vector3(wallData.scale[0],wallData.scale[1],wallData.scale[2]);
            ((WallBuilderTool)toolsByName["WallBuilder"]).createWall(wallPos,wallRot,wallScale);
        }
    }
    private void loadSimulation(SimulationData data)
    {
        disableTool(selectedTool);
        
        foreach(EvacueeData evacueeData in data.evacData)
        {
            Vector3 evacPos = new Vector3(evacueeData.position[0],evacueeData.position[1],evacueeData.position[2]);
            Quaternion evacRot = new Quaternion(evacueeData.rotation[0],evacueeData.rotation[1],evacueeData.rotation[2],evacueeData.rotation[3]);
            ((EvacueeFactoryTool)toolsByName["EvacueeFactory"]).createObject(evacPos,evacRot);
        }
        foreach(FireExitData fireExitData in data.fireExitData)
        {
            Vector3 fePos = new Vector3(fireExitData.position[0],fireExitData.position[1],fireExitData.position[2]);
            Quaternion feRot = new Quaternion(fireExitData.rotation[0],fireExitData.rotation[1],fireExitData.rotation[2],fireExitData.rotation[3]);
            ((FireExitFactoryTool)toolsByName["FireExitFactory"]).createObject(fePos,feRot);
        }
        foreach(WindowData winData in data.winData)
        {
            Vector3 winPos = new Vector3(winData.position[0],winData.position[1],winData.position[2]);
            Quaternion winRot = new Quaternion(winData.rotation[0],winData.rotation[1],winData.rotation[2],winData.rotation[3]);
            ((WindowFactoryTool)toolsByName["WindowFactory"]).createObject(winPos,winRot);
        }
        foreach(WallData wallData in data.wallData)
        {
            Vector3 wallPos = new Vector3(wallData.position[0],wallData.position[1],wallData.position[2]);
            Quaternion wallRot = new Quaternion(wallData.rotation[0],wallData.rotation[1],wallData.rotation[2],wallData.rotation[3]);
            Vector3 wallScale = new Vector3(wallData.scale[0],wallData.scale[1],wallData.scale[2]);
            ((WallBuilderTool)toolsByName["WallBuilder"]).createWall(wallPos,wallRot,wallScale);
        }
    }
    
}

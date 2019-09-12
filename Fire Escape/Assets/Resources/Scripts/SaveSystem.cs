using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SFB;

public static class SaveSystem
{
    //static string fileName = "/evacuee.evac";
    static string path ="";
    static string gameFileExtension = "evac";

    public static void SaveSimData(SimulationObjectLists simObjLists)
    {
        string savePanelTitle = "Save the Fire Egress Simulation";
        string defaultName = "FireEgress";
        path = StandaloneFileBrowser.SaveFilePanel(savePanelTitle,"",defaultName,gameFileExtension);
        //EditorUtility.SaveFilePanel(savePanelTitle, "", defaultName , fileExtension);
        //if the path is not selected (user selected cancel or clicked the x)
        if (path.Length == 0)
        {
            //respond to nothing being selected
        }
        //if a file was selected
        else
        {
            //respond to the file being selected
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path,FileMode.Create);

            SimulationData simData = new SimulationData(simObjLists);

            formatter.Serialize(stream,simData);
            stream.Close();
            path = "";
        }


        /*BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + fileName;
        FileStream stream = new FileStream(path,FileMode.Create);

        SimulationData simData = new SimulationData(simObjLists);

        formatter.Serialize(stream,simData);
        stream.Close();*/
    }
    public static SimulationData LoadSimData()
    {
        string loadPanelTitle = "Load Fire Egress Simulation'";
        

        //string title, string directory, string extension, bool multiselect
        string[] openResults = StandaloneFileBrowser.OpenFilePanel(loadPanelTitle, "", gameFileExtension, false);
        //if there were results
        if(openResults.Length > 0)
        {
            path = openResults[0];
            //if the path is not selected (user selected cancel or clicked the x)
            if(path.Length ==0)
            {
                //respond to nothing being selected
                return null;
            }
            //if a file was selected
            else
            {
                //respond to the file being selected
                //if there is a file there
                if(File.Exists(path))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    FileStream stream = new FileStream(path, FileMode.Open);
                    SimulationData data = formatter.Deserialize(stream) as SimulationData;
                    stream.Close();
                    //Debug.Log("Load Path is: " + path);
                    path = "";
                    return data;
                }
                else
                {
                    //Debug.LogError("Save file not found in " + path);
                    path = "";
                    return null;
                }
            }
        }
        else
        {
            //handle nothing being selected
            //Debug.Log("Nothing was selected!");
            return null;
        }
        


        /*string path = Application.persistentDataPath + fileName;
        if(File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);
            SimulationData data = formatter.Deserialize(stream) as SimulationData;
            stream.Close();

            return data;
        }
        else
        {
            Debug.LogError("Save file not found in " + path);
            return null;
        }*/
    }
    /* public static void SaveEvacuee(GameObject evacuee)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + fileName;
        FileStream stream = new FileStream(path,FileMode.Create);

        EvacueeData evacData = new EvacueeData(evacuee);

        formatter.Serialize(stream,evacData);
        stream.Close();
    }
    public static EvacueeData LoadEvacuee()
    {
        string path = Application.persistentDataPath + fileName;
        if(File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);
            EvacueeData data = formatter.Deserialize(stream) as EvacueeData;
            stream.Close();

            return data;
        }
        else
        {
            Debug.LogError("Save file not found in " + path);
            return null;
        }
    }*/
}

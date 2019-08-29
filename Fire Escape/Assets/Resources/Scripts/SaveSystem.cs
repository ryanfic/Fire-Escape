using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    static string fileName = "/evacuee.evac";

    public static void SaveSimData(SimulationObjectLists simObjLists)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + fileName;
        FileStream stream = new FileStream(path,FileMode.Create);

        SimulationData simData = new SimulationData(simObjLists);

        formatter.Serialize(stream,simData);
        stream.Close();
    }
    public static SimulationData LoadSimData()
    {
        string path = Application.persistentDataPath + fileName;
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
        }
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

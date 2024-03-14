using System.IO;

public static class DecisionSaver 
{
    public static void SaveDecision(string filePath, string content)
    {
        using (StreamWriter outputFile = new StreamWriter(filePath, true))
        {
            outputFile.WriteLine(content);
        }
    }
}

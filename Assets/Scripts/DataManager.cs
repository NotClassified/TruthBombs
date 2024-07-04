using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    //========================================================================
    public static DataManager singleton;

    QuestionData m_currentData;
    string defaultQuestionsPath;

    //========================================================================
    private void Awake()
    {
        singleton = this;
    }
    private void Start()
    {
        defaultQuestionsPath = Application.persistentDataPath + "/DefaultQuestions.json";

        if (!DataExists())
        {
            m_currentData = new QuestionData();
            SaveData();
        }
        ReadData();
        Debug.Log("File Location: " + Application.persistentDataPath);
    }

    //========================================================================
    void SaveData()
    {
        //DeleteDataFile(); //for overwriting

        using (FileStream stream = File.Open(defaultQuestionsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(ConvertDataToJson(false));
                writer.Flush(); // applies the changes to the file
            }
        }
    }
    void ReadData()
    {
        if (!DataExists())
        {
            Debug.LogWarning("No data");
            return;
        }

        using (FileStream stream = File.Open(defaultQuestionsPath, FileMode.Open, FileAccess.ReadWrite))
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                m_currentData = JsonUtility.FromJson<QuestionData>(ConvertFileToJson(reader, false));
                LoadData();
            }
        }
    }
    void LoadData()
    {
        string logMessage = "";
        foreach (string question in m_currentData.questions)
        {
            logMessage += question.ToString();
        }
        Debug.Log(logMessage);
    }
    bool DataExists() => File.Exists(defaultQuestionsPath);

    //========================================================================
    string ConvertDataToJson(bool print)
    {
        string json = JsonUtility.ToJson(m_currentData, true);
        if (print)
            Debug.Log("Serialized data: \n" + json);

        return json;
    }
    string ConvertFileToJson(StreamReader reader, bool print)
    {
        string json = reader.ReadToEnd();
        if (print)
            Debug.Log("Previously Saved Data: \n" + json);

        return json;
    }

    //========================================================================
    public int GetQuestionCount() => m_currentData.questions.Length;
    public FixedString128Bytes GetQuestion(int index) => m_currentData.questions[index];
    public int GetRandomQuestion(List<int> excludeIndexes)
    {
        int randomIndex = -1;
        bool valid = false;

        int questionAmount = m_currentData.questions.Length;
        int inifiniteCount = 0;
        while (!valid && inifiniteCount++ < 1000000)
        {
            randomIndex = Random.Range(0, questionAmount);

            valid = true;
            foreach (int exclude in excludeIndexes)
            {
                if (exclude == randomIndex)
                {
                    valid = false;
                    break;
                }
            }
        }
        if (inifiniteCount >= 1000000)
            Debug.LogError("infinite loop detected");

        return randomIndex;
    }

    //========================================================================
}

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
    string m_defaultQuestionsPath;

    //========================================================================
    private void Awake()
    {
        singleton = this;
    }
    private void Start()
    {
        m_defaultQuestionsPath = Application.persistentDataPath + "/DefaultQuestions.json";

        if (!DataExists())
        {
            m_currentData = new QuestionData();
            SaveData();
        }
        ReadData();
        //Debug.Log("File Location: " + Application.persistentDataPath);
    }

    //========================================================================
    void SaveData()
    {
        //DeleteDataFile(); //for overwriting

        using (FileStream stream = File.Open(m_defaultQuestionsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
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

        using (FileStream stream = File.Open(m_defaultQuestionsPath, FileMode.Open, FileAccess.ReadWrite))
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
    }
    bool DataExists() => File.Exists(m_defaultQuestionsPath);

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
    /// <summary>
    /// replaces tokens in questions such as "&lt;leftPlayer&gt;" which will be the left player's name
    /// </summary>
    public static string ReplaceTokens(string question, int targetPlayerIndex)
    {
        if (question.Contains("<leftPlayer>"))
        {
            int leftPlayerIndex = PlayerManager.GetPlayerIndex(targetPlayerIndex, 1);
            string tokenValue = PlayerManager.GetPlayerName(leftPlayerIndex).ToString();

            question = question.Replace("<leftPlayer>", tokenValue);
        }
        if (question.Contains("<rightPlayer>"))
        {
            int rightPlayerIndex = PlayerManager.GetPlayerIndex(targetPlayerIndex, -1);
            string tokenValue = PlayerManager.GetPlayerName(rightPlayerIndex).ToString();

            question = question.Replace("<rightPlayer>", tokenValue);
        }
        return question;
    }

    public int GetRandomQuestion(List<int> excludeIndexes, bool allowTokens = true)
    {
        int randomIndex = -1;
        bool valid = false;

        int questionAmount = m_currentData.questions.Length;
        int inifiniteCount = 0;
        while (!valid && inifiniteCount++ < 1000000)
        {
            randomIndex = UnityEngine.Random.Range(0, questionAmount);

            valid = true;
            foreach (int exclude in excludeIndexes)
            {
                if (exclude == randomIndex)
                {
                    valid = false;
                    break;
                }
            }

            if (!allowTokens)
            {
                string question = m_currentData.questions[randomIndex];
                if (question.Contains("<") && question.Contains(">"))
                    valid = false;
            }
        }
        if (inifiniteCount >= 1000000)
            Debug.LogError("infinite loop detected");

        return randomIndex;
    }

    //========================================================================
}

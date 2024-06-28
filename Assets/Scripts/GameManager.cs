using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using static AnswerSheet;

public class GameManager : MonoBehaviour
{
    public static GameManager singleton;

    public List<AnswerSheet> answerSheets = new List<AnswerSheet>();
    public List<int> currentquestionCardIndexes = new List<int>();
    public FixedString128Bytes[] questionCards = {
        "one",
        "two",
        "three",
        "four"
    };

    private void Awake()
    {
        singleton = this;

        PlayerManager.PlayerAdded += AddPlayerAnswerSheet;
    }

    void AddPlayerAnswerSheet()
    {
        answerSheets.Add(new AnswerSheet());
    }

    public void AddAnswer(int answerSheetIndex, int cardIndex, CardAnswer newAnswer)
    {
        answerSheets[answerSheetIndex].cardAnswers[cardIndex] = newAnswer;
    }
}

public class AnswerSheet
{
    public class CardAnswer
    {
        public int answeringPlayerIndex = -1;
        public FixedString128Bytes answerString;
    }
    public List<CardAnswer> cardAnswers = new List<CardAnswer>();
}

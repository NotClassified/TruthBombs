using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using System;
using TMPro;
using System.Reflection;

public class State_AnswerSheet : StateBase
{
    int selectedQuestionCardIndex = 0;

    public Transform questionCardParent;
    public GameObject questionCardPrefab;
    List<Button> m_questionCardButtons = new List<Button>();

    int currentAnswerSheetindex;
    TMP_InputField answerInput;

    public override void OnEnter()
    {
        base.OnEnter();

        currentAnswerSheetindex = Player.owningPlayer.playerIndex;
        NextAnswerSheet();

        m_questionCardButtons.Clear();

        foreach (FixedString128Bytes card in GameManager.singleton.GetCurrentQuestionCards())
        {
            GameObject cardObject = Instantiate(questionCardPrefab, questionCardParent);
            m_questionCardButtons.Add(cardObject.GetComponent<Button>());

            TextMeshProUGUI cardText = cardObject.GetComponentInChildren<TextMeshProUGUI>();
            cardText.text = card.ToString();
        }

        for (int i = 0; i < m_questionCardButtons.Count; i++)
        {
            int index = i;
            m_questionCardButtons[i].onClick.RemoveAllListeners();
            m_questionCardButtons[i].onClick.AddListener(() => { SelectQuestionCard(index); });
        }
    }

    void NextAnswerSheet()
    {
        if (++currentAnswerSheetindex >= PlayerManager.singleton.allPlayers.Count)
            currentAnswerSheetindex = 0;
    }

    void SelectQuestionCard(int index)
    {
        m_questionCardButtons[selectedQuestionCardIndex].interactable = true; //reset previous selected card

        selectedQuestionCardIndex = index;
        m_questionCardButtons[index].interactable = false;
    }

    public void ConfirmAnswer()
    {
        Player.owningPlayer.AddAsnwer_ServerRpc(Player.owningPlayer.playerIndex, currentAnswerSheetindex, selectedQuestionCardIndex, answerInput.text);
        

    }
}

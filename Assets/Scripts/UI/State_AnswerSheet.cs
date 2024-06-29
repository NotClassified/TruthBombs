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
    //========================================================================
    public Transform questionCardParent;
    public GameObject questionCardPrefab;
    List<Button> m_questionCardButtons = new();

    //========================================================================
    public Color selectedCardColor;
    public Color unselectedCardColor;
    public Color unavailableCardColor;

    int m_selectedQuestionCardIndex = -1;

    List<AnswerSheet> m_pendingAnswerSheets = new();

    public TMP_InputField answerInput;
    string m_currentAnswerInput;


    //========================================================================
    public override void OnEnter()
    {
        base.OnEnter();

        GameManager.NewPendingAnswerSheet += AddAnswerSheet;
        GameManager.NoMorePendingAnswerSheets += NoMorePendingAnswerSheets;

        //set question cards
        {
            m_questionCardButtons.Clear();

            foreach (FixedString128Bytes card in GameManager.singleton.GetCurrentQuestionCards())
            {
                GameObject cardObject = Instantiate(questionCardPrefab, questionCardParent);
                m_questionCardButtons.Add(cardObject.GetComponent<Button>());

                cardObject.GetComponent<Button>().image.color = unselectedCardColor;

                TextMeshProUGUI cardText = cardObject.GetComponentInChildren<TextMeshProUGUI>();
                cardText.text = card.ToString();
            }

            for (int i = 0; i < m_questionCardButtons.Count; i++)
            {
                int index = i;
                m_questionCardButtons[i].onClick.AddListener(() => { SelectQuestionCard(index); });
            }
        }
    }
    public override void OnExit()
    {
        base.OnExit();

        GameManager.NewPendingAnswerSheet -= AddAnswerSheet;
        GameManager.NoMorePendingAnswerSheets -= NoMorePendingAnswerSheets;

        for (int i = 0; i < m_questionCardButtons.Count; i++)
        {
            Destroy(m_questionCardButtons[i].gameObject);
        }
        m_questionCardButtons.Clear();
    }

    //========================================================================
    void AddAnswerSheet(AnswerSheet newSheet)
    {
        m_pendingAnswerSheets.Add(newSheet);

        //first/only pending answer sheet?
        if (m_pendingAnswerSheets.Count == 1) 
        {
            SetCurrentAnswerSheet();
        }
    }
    void SetCurrentAnswerSheet()
    {
        //question card indexes and answer indexes should match
        for (int i = 0; i < m_pendingAnswerSheets[0].cardAnswers.Count; i++)
        {
            //is this question card already answered?
            if (m_pendingAnswerSheets[0].cardAnswers[i].answeringPlayerIndex != -1)
            {
                m_questionCardButtons[i].interactable = false;
                m_questionCardButtons[i].image.color = unavailableCardColor;
            }
            else
            {
                m_questionCardButtons[i].interactable = true;
                m_questionCardButtons[i].image.color = unselectedCardColor;
            }
        }

        m_selectedQuestionCardIndex = -1;

        answerInput.interactable = true;
        answerInput.text = "";
        m_currentAnswerInput = "";
    }

    //========================================================================
    void SelectQuestionCard(int cardIndex)
    {
        UnselectCard(m_selectedQuestionCardIndex); //reset previous selected card

        m_selectedQuestionCardIndex = cardIndex;
        m_questionCardButtons[cardIndex].image.color = selectedCardColor;
    }
    void UnselectCard(int cardIndex)
    {
        if (m_selectedQuestionCardIndex == -1)
            return; //there wasn't a previous selected card

        m_questionCardButtons[cardIndex].image.color = unselectedCardColor;
    }

    //========================================================================
    public void ConfirmAnswer()
    {
        //Player.owningPlayer.AddAsnwer_ServerRpc(Player.owningPlayer.playerIndex, currentAnswerSheetindex, selectedQuestionCardIndex, answerInput.text);

        if (m_pendingAnswerSheets.Count == 0)
            return; //there are no pending answer sheets
        if (m_selectedQuestionCardIndex == -1)
            return; //there isn't a selected card

        GameManager.singleton.SyncAnswerSheet(
            m_pendingAnswerSheets[0].targetPlayerIndex, 
            m_selectedQuestionCardIndex, 
            m_currentAnswerInput);

        m_pendingAnswerSheets.RemoveAt(0); //remove the confirmed answer sheet
        //is there still pending answer sheets left?
        if (m_pendingAnswerSheets.Count > 0)
        {
            SetCurrentAnswerSheet(); //go to the next answer sheet
        }
        else
        {
            WaitForAnswerSheet();
        }
    }
    void WaitForAnswerSheet()
    {
        //disable all input:
        for (int i = 0; i < m_questionCardButtons.Count; i++)
        {
            m_questionCardButtons[i].interactable = false;
            m_questionCardButtons[i].image.color = unavailableCardColor;
        }

        answerInput.interactable = false;
        answerInput.text = "";
        m_currentAnswerInput = "";
    }
    void NoMorePendingAnswerSheets()
    {
        UIManager.singleton.ChangeUIState<State_WaitForAnswers>();
    }

    //========================================================================
    public void SetAnswerInput(string newInput)
    {
        if (newInput.Length > 128)
            return; //prevent a name that exceeds the "FixedString128Bytes" size

        m_currentAnswerInput = newInput;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UIState
{
    public class StateBase : MonoBehaviour
    {
        protected bool isCurrentState = false;

        public virtual void OnEnter()
        {
            isCurrentState = true;
            gameObject.SetActive(true);
        }
        public virtual void OnExit()
        {
            isCurrentState = false;
            gameObject.SetActive(false);
        }
    }
}
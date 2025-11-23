using UnityEngine;

namespace Manager.TurnState
{
    public class TurnGameOverState : ITurnState
    {
        private TurnManager turnManager;

        public TurnGameOverState(TurnManager turnManager)
        {
            this.turnManager = turnManager;
        }

        public void OnStateEnter()
        {
            LogManager.Instance.Log("Entering TurnGameOverState");
        }

        public void OnStateExit()
        {
            LogManager.Instance.Log("Exiting TurnGameOverState");
        }
    }
}
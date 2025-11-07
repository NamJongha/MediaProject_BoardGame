namespace Manager.TurnState
{
    public class WaitingForOrderState : ITurnState
    {
        private TurnManager turnManager;

        public WaitingForOrderState(TurnManager turnManager)
        {
            this.turnManager = turnManager;
        }
        
        public void OnStateEnter()
        {
            LogManager.Instance.Log("Entering WaitingForOrderState");
            turnManager.ShowTurnDecideButton(true);
            turnManager.ShowTurnStartButton(false);
        }

        public void OnStateExit()
        {
            LogManager.Instance.Log("Exiting WaitingForOrderState");
            turnManager.ShowTurnDecideButton(false);
        }
    }
}
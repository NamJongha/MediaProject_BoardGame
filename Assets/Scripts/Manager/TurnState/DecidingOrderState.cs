namespace Manager.TurnState
{
    public class DecidingOrderState : ITurnState
    {
        private TurnManager turnManager;

        public DecidingOrderState(TurnManager turnManager)
        {
            this.turnManager = turnManager;
        }
        public void OnStateEnter()
        {
            LogManager.Instance.Log("Entering DecidingOrderState");
            turnManager.ShowTurnDecideButton(false);
            turnManager.ShowTurnStartButton(true);
        }

        public void OnStateExit()
        {
            LogManager.Instance.Log("Exiting DecidingOrderState");
            turnManager.ChangeState(new TurnStartState(turnManager));
        }
    }
}
namespace Manager.TurnState
{
    public class TurnStartState : ITurnState
    {
        private TurnManager turnManager;

        public TurnStartState(TurnManager turnManager)
        {
            this.turnManager = turnManager;
        }
        
        public void OnStateEnter()
        {
            LogManager.Instance.Log("Entering TurnStartState");
            turnManager.ShowTurnDecideButton(false);
            turnManager.ShowTurnStartButton(false);
        }

        public void OnStateExit()
        {
            LogManager.Instance.Log("Exiting TurnStartState");
            turnManager.ChangeState(new TurnActionState(turnManager));
        }
    }
}
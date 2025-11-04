namespace Manager.TurnState
{
    public class TurnActionState : ITurnState
    {
        private TurnManager turnManager;
        
        public TurnActionState(TurnManager turnManager)
        {
            this.turnManager = turnManager;
        }
        
        public void OnStateEnter()
        {
            LogManager.Instance.Log("Entering TurnActionState");
        }

        public void OnStateExit()
        {
            LogManager.Instance.Log("Exiting TurnActionState");
            turnManager.ChangeState(new TurnEndState(turnManager));
        }
    }
}
namespace Manager.TurnState
{
    public class TurnEndState : ITurnState
    {
        private TurnManager turnManager;
        public TurnEndState(TurnManager turnManager)
        {
            this.turnManager = turnManager;
        }
        
        public void OnStateEnter()
        {
            //LogManager.Instance.Log("Entering TurnEndState");
        }

        public void OnStateExit()
        {
            //LogManager.Instance.Log("Exiting TurnEndState");
        }
    }
}
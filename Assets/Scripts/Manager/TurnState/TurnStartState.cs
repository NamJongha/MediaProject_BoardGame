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
            /*
            Player currentPlayer = turnManager.GetCurrentTurnPlayer();

            if (currentPlayer != null)
            {
                currentPlayer.StartDiceRoll();
            }
            
            turnManager.ChangeState(new TurnActionState(turnManager));
            */
        }

        public void OnStateExit()
        {
            LogManager.Instance.Log("Exiting TurnStartState");
        }
    }
}
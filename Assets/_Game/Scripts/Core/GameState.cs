namespace BallCatch.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        AttemptTransition,
        Paused,
        Settings,
        ConfirmReturnToMenu
    }

    public enum AttemptResult
    {
        Catch,
        Miss
    }
}

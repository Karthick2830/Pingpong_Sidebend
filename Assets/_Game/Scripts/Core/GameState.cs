namespace BallCatch.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        AttemptTransition,
        Paused,
        Settings,
        ConfirmReturnToMenu,
        ExerciseComplete
    }

    public enum AttemptResult
    {
        Catch,
        Miss
    }
}

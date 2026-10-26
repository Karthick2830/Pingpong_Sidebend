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
        ExerciseComplete,
        HowToPlay
    }

    public enum AttemptResult
    {
        Catch,
        Miss
    }
}

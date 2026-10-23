using UnityEngine;

namespace BallCatch.Gameplay
{
    /// <summary>Marks a side wall so collision handling can identify it without name or tag checks.</summary>
    public class WallSurface : MonoBehaviour
    {
        [SerializeField] private TargetSide side;

        public TargetSide Side => side;
    }
}

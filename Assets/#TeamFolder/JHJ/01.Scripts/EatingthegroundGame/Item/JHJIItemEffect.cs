using JHJ.Scripts.Test.TestPlayer;

namespace JHJ.Scripts.EatingthegroundGame
{
    public interface JHJIItemEffect
    {
        void Apply(JHJPlayerController player);
        void Remove(JHJPlayerController player);
    }
}
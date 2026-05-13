using JHJ.Scripts.Test.TestPlayer;
using UnityEngine;

namespace JHJ.Scripts.EatingthegroundGame
{
    public interface JHJIItemEffect
    {
        void Apply(JHJPlayerController player); //발동
        void Remove(JHJPlayerController player);//끝
    }
}


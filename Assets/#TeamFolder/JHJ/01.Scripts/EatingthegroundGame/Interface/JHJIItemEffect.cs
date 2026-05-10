using JHJ.Scripts.Test.TestPlayer;
using UnityEngine;

namespace JHJ.Scripts.EatingthegroundGame
{
    public interface JHJIItemEffect
    {
        void Apply(TestPlayerController player); //발동
        void Remove(TestPlayerController player);//끝
    }
}


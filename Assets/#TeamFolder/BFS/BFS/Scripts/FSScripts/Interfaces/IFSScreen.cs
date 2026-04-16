using UnityEngine;
namespace BFS
{
    public interface IFSScreen                               // Interface for monitor screen in minigame
    {
        void ChangeScreenColor(Color color);                 // Method that gets Color as parameter. Used when changing monitor screen
        void ResetScreenColor();                             // Method, sets the monitor screen back to its default
    }
}

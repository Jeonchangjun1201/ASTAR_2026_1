using UnityEngine;

namespace KSY.Shared.UI
{
    public interface IView 
    { 
        public string Name { get; }

        public void Show(string info);
        public void Show(string info, Color color);

        public void Hide();
    }
}

namespace _TeamFolder.PYH._02.Scripts
{
    public class SetUiInputEvent
    {
        public bool CanInput { get; private set; }

        public SetUiInputEvent(bool canInput)
        {
            CanInput = canInput;
        }
    }
}
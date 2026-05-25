using _TeamFolder.PYH._02.Scripts.UI;

public class GameResultUiEvent
{
    public ResultData[] Results { get; private set; }

    public GameResultUiEvent(ResultData[] results)
    {
        Results = results;
    }
}

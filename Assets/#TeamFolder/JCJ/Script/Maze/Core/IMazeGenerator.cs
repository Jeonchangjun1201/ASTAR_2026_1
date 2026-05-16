

// 격자 기반 미로 생성기 계약 인터페이스.

namespace _TeamFolder.JCJ.Script
{
    /// <summary>
    /// 미로 생성 알고리즘이 동일한 seed로 재현 가능한 벽/통로 배열을 만들기 위한 계약.
    /// </summary>
    public interface IMazeGenerator
    {
        // 0 = 통로, 1 = 벽. 동일 seed에 동일 결과 보장.
        int[,] Generate(int width, int height, int seed);
    }
}

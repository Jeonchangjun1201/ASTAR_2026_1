using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
    public interface IMazeGenerator
    {
        int[,] Generate(int width, int height); //0=길,1=벽
    }
}

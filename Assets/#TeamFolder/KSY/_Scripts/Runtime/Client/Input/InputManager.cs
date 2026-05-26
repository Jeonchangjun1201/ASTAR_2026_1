using System;
using System.Collections.Generic;

namespace KSY.Clients
{
    public static class InputManager
    {
        private static readonly Dictionary<Type, InputReaderBase> inputReaders = null;
        private static KSY_InputActions inputActions = null;
        private static Type currentInputReaderType = null;

        public static bool CanInput = false;

        static InputManager()
        {
            inputReaders = new Dictionary<Type, InputReaderBase>();
            inputActions = new KSY_InputActions();
            currentInputReaderType = null;
        }

        public static void Initialize()
        {
            currentInputReaderType = null;
        }

        public static void SetInput(bool canInput) => CanInput = canInput;

        public static void Release()
        {
            foreach (InputReaderBase inputReader in inputReaders.Values)
                inputReader.Release();

            inputReaders.Clear();
            inputActions.Dispose();
            inputActions = null;
        }

        public static TInputReader GetInput<TInputReader>() where TInputReader : InputReaderBase
        {
            Type inputReaderType = typeof(TInputReader);
            if (inputReaders.TryGetValue(inputReaderType, out InputReaderBase inputReader) == false)
                return null;

            return inputReader as TInputReader;
        }

        public static void EnableInput<TInputReader>() where TInputReader : InputReaderBase, new()
        {
            Type inputReaderType = typeof(TInputReader);
            if(inputReaders.TryGetValue(inputReaderType, out InputReaderBase inputReader) == false)
            {
                inputReader = new TInputReader();
                inputReader.Initialize(inputActions);
                inputReaders.Add(inputReaderType, inputReader);
            }

            if (inputReaderType == currentInputReaderType)
                return;

            if (currentInputReaderType != null)
                inputReaders[currentInputReaderType].GetInputActionMap().Disable();

            currentInputReaderType = inputReaderType;
            inputReader.GetInputActionMap().Enable();
        }

        public static void DisableInput()
        {
            if (currentInputReaderType != null)
                inputReaders[currentInputReaderType].GetInputActionMap().Disable();

            currentInputReaderType = null;
        }
    }
}


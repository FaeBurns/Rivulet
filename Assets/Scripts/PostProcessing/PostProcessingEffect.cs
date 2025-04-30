using System;
using UnityEngine;

namespace PostProcessing
{
    [Serializable]
    public struct PostProcessingEffect
    {
        public Shader Shader;
        public PostProcessingEffectParameter[] Parameters;
    }

    [Serializable]
    public struct PostProcessingEffectParameter
    {
        public EffectParameterType Type;
        public string Name;
        public bool FlagValue;
        public int IntValue;
        public float FloatValue;
        public Color ColorValue;
    }

    public enum EffectParameterType
    {
        FLAG,
        INT,
        FLOAT,
        COLOR,
    }
}
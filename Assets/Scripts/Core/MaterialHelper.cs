using UnityEngine;

namespace ThermalPumpDT.Core
{
    /// <summary>Resolves a lit shader for URP or Built-in pipeline.</summary>
    public static class MaterialHelper
    {
        public static Material CreateLitMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;
            return mat;
        }
    }
}

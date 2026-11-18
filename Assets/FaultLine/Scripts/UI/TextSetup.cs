using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace FaultLine
{
    /// <summary>
    /// Arabic needs Unity 6's Advanced Text Generator (shaping + right-to-left).
    /// Set via reflection so the project compiles on any Unity 6 minor version.
    /// Resources/ui.uss sets the same thing in USS as a fallback.
    /// </summary>
    public static class TextSetup
    {
        public static bool AtgOk, DirOk;
        public static void Apply(VisualElement root)
        {
            var sheet = Resources.Load<StyleSheet>("ui"); if (sheet != null && !root.styleSheets.Contains(sheet)) root.styleSheets.Add(sheet);
            try
            {
                var p = typeof(IStyle).GetProperty("unityTextGenerator");
                if (p != null)
                {
                    var styleEnumType = p.PropertyType; var enumType = styleEnumType.GetGenericArguments()[0];
                    var adv = Enum.Parse(enumType, "Advanced"); var val = Activator.CreateInstance(styleEnumType, adv);
                    p.SetValue(root.style, val); AtgOk = true;
                }
            }
            catch (Exception e) { Debug.LogWarning("Fault Line: advanced text generator not set: " + e.Message); }
            SetDir(root, Save.Ar);
        }
        public static void SetDir(VisualElement e, bool rtl)
        {
            try
            {
                var p = typeof(VisualElement).GetProperty("languageDirection");
                if (p != null) { p.SetValue(e, Enum.Parse(p.PropertyType, rtl ? "RTL" : "LTR")); DirOk = true; }
            }
            catch (Exception ex) { Debug.LogWarning("Fault Line: language direction not set: " + ex.Message); }
        }
    }
}

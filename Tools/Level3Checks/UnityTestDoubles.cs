// Test-only stand-ins: these do not simulate Unity physics, audio playback or rendering.
// They let the console checks exercise the real GameManager and GameSession code.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object { public static void Destroy(Object value) { } }
    public class MonoBehaviour : Object
    {
        public GameObject gameObject = new GameObject();
        public T GetComponent<T>() where T : class { return gameObject.GetComponent<T>(); }
    }
    public class GameObject
    {
        private readonly Dictionary<Type, object> components = new Dictionary<Type, object>();
        public T GetComponent<T>() where T : class
        {
            return components.TryGetValue(typeof(T), out object value) ? (T)value : null;
        }
        public T AddComponent<T>() where T : class, new()
        {
            T value = new T();
            components[typeof(T)] = value;
            return value;
        }
    }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RequireComponent : Attribute { public RequireComponent(Type type) { } }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinAttribute : Attribute { public MinAttribute(float value) { } }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute { }

    public class AudioClip : Object { }
    public class AudioSource : Object
    {
        public bool playOnAwake;
        public float spatialBlend, volume;
        public readonly List<AudioClip> Played = new List<AudioClip>();
        public void PlayOneShot(AudioClip clip) { Played.Add(clip); }
    }
    public static class AudioListener { public static bool pause; }
    public class Sprite : Object { }
    public class SpriteRenderer : Object { public Sprite sprite; public Color color; }
    public static class PlayerPrefs
    {
        public static readonly Dictionary<string, int> Values = new Dictionary<string, int>();
        public static int Saves;
        public static int GetInt(string key, int fallback = 0)
        {
            return Values.TryGetValue(key, out int value) ? value : fallback;
        }
        public static void SetInt(string key, int value) { Values[key] = value; }
        public static void Save() { Saves++; }
        public static void ResetForTest() { Values.Clear(); Saves = 0; }
    }
    public static class Time { public static float timeScale = 1f, deltaTime = 0.02f; }
    public enum KeyCode { P, R }
    public static class Input
    {
        public static readonly HashSet<KeyCode> Down = new HashSet<KeyCode>();
        public static bool GetKeyDown(KeyCode key) { return Down.Contains(key); }
    }
    public static class Debug { public static void Log(object message) { } }
    public static class Mathf
    {
        public static int Clamp(int value, int low, int high) { return Math.Max(low, Math.Min(high, value)); }
        public static int Max(int a, int b) { return Math.Max(a, b); }
        public static int Min(int a, int b) { return Math.Min(a, b); }
        public static float Min(float a, float b) { return Math.Min(a, b); }
        public static int CeilToInt(float value) { return (int)Math.Ceiling(value); }
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float red, float green, float blue, float alpha = 1f) { r=red; g=green; b=blue; a=alpha; }
        public static Color white => new Color(1, 1, 1);
        public static Color green => new Color(0, 1, 0);
        public static Color clear => new Color(0, 0, 0, 0);
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x=x; this.y=y; this.z=z; }
        public static Vector3 zero => new Vector3(0, 0, 0);
    }
    public struct Quaternion { public static Quaternion identity => new Quaternion(); }
    public struct Matrix4x4
    {
        public static Matrix4x4 TRS(Vector3 position, Quaternion rotation, Vector3 scale) { return new Matrix4x4(); }
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        { this.x=x; this.y=y; this.width=width; this.height=height; }
    }
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Point }
    public enum FontStyle { Bold }
    public class Texture2D : Object
    {
        private int width, height;
        public FilterMode filterMode;
        public static Texture2D whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        public Texture2D(int width, int height, TextureFormat format, bool mipmap) { this.width=width; this.height=height; }
        public void SetPixel(int x, int y, Color color)
        { if (x<0 || y<0 || x>=width || y>=height) throw new Exception("Texture bounds"); }
        public void Apply() { }
    }
    public class GUIStyleState { public Color textColor; }
    public class GUIStyle
    {
        public int fontSize;
        public FontStyle fontStyle;
        public GUIStyleState normal = new GUIStyleState();
        public GUIStyle() { }
        public GUIStyle(GUIStyle other) { }
    }
    public class GUISkin { public GUIStyle label = new GUIStyle(), button = new GUIStyle(); }
    public static class Screen { public static int width = 1920, height = 1080; }
    public static class GUI
    {
        public static Matrix4x4 matrix;
        public static Color color = Color.white;
        public static bool enabled = true;
        public static GUISkin skin = new GUISkin();
        public static void Label(Rect rect, string text, GUIStyle style) { }
        public static void DrawTexture(Rect rect, Texture2D texture) { }
        public static bool Button(Rect rect, string text, GUIStyle style) { return false; }
    }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public string name; }
    public static class SceneManager
    {
        public static string LastLoaded;
        public static Scene GetActiveScene() { return new Scene { name = "EscapeLab2D" }; }
        public static void LoadScene(string name) { LastLoaded = name; }
    }
}

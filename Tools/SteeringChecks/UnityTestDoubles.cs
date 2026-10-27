// Test-only Unity stand-ins. These compile/exercise controller decisions, not real physics.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        private static int nextId;
        private readonly int id = ++nextId;
        public int GetInstanceID() { return id; }
        public static T FindAnyObjectByType<T>() where T : class { return TestScene.Manager as T; }
    }
    public static class TestScene { public static EscapeLab2D.GameManager Manager; }
    public class MonoBehaviour : Object
    {
        public readonly GameObject gameObject = new GameObject();
        public Transform transform => gameObject.transform;
        public T GetComponent<T>() where T : class { return gameObject.GetComponent<T>(); }
    }
    public class GameObject : Object
    {
        public readonly Transform transform = new Transform();
        private readonly Dictionary<Type,object> components = new Dictionary<Type,object>();
        public T AddComponent<T>() where T : Collider2D, new()
        { var value = new T { Owner=this }; components[typeof(T)]=value; return value; }
        public T GetComponent<T>() where T : class
        { return components.TryGetValue(typeof(T), out object value) ? (T)value : null; }
    }
    public class Transform : Object
    {
        public Vector3 position;
        public Vector3 localScale=new Vector3(1,1,1);
        public Vector3 TransformPoint(Vector3 value)=>position+
            new Vector3(value.x*localScale.x,value.y*localScale.y,value.z*localScale.z);
    }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RequireComponent : Attribute { public RequireComponent(Type type) { } }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HeaderAttribute : Attribute { public HeaderAttribute(string text) { } }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinAttribute : Attribute { public MinAttribute(float value) { } }
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute { }

    public struct Vector2
    {
        public float x,y;
        public Vector2(float x,float y) { this.x=x;this.y=y; }
        public float sqrMagnitude => x*x+y*y;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public Vector2 normalized => magnitude>0.00001f ? this/magnitude : zero;
        public static Vector2 zero => new Vector2(0,0);
        public static Vector2 right => new Vector2(1,0);
        public static Vector2 left => new Vector2(-1,0);
        public static Vector2 up => new Vector2(0,1);
        public static Vector2 down => new Vector2(0,-1);
        public static Vector2 operator +(Vector2 a,Vector2 b) => new Vector2(a.x+b.x,a.y+b.y);
        public static Vector2 operator -(Vector2 a,Vector2 b) => new Vector2(a.x-b.x,a.y-b.y);
        public static Vector2 operator -(Vector2 a) => new Vector2(-a.x,-a.y);
        public static Vector2 operator *(Vector2 a,float b) => new Vector2(a.x*b,a.y*b);
        public static Vector2 operator *(float b,Vector2 a) => a*b;
        public static Vector2 operator /(Vector2 a,float b) => new Vector2(a.x/b,a.y/b);
        public static float Dot(Vector2 a,Vector2 b) => a.x*b.x+a.y*b.y;
        public static float Distance(Vector2 a,Vector2 b) => (a-b).magnitude;
        public static Vector2 ClampMagnitude(Vector2 v,float max) => v.magnitude>max ? v.normalized*max : v;
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x,v.y,0);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x,v.y);
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 operator +(Vector3 a,Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
    }
    public struct Rect
    {
        public float x,y,width,height;
        public Rect(float x,float y,float width,float height)
        { this.x=x;this.y=y;this.width=width;this.height=height; }
        public float xMin=>x;
        public float xMax=>x+width;
        public float yMin=>y;
        public float yMax=>y+height;
    }
    public struct Bounds { public Vector3 center,extents; }
    public class Collider2D : Object
    {
        public GameObject Owner;
        public float Radius=0.25f;
        public Vector2 offset;
        public Func<Vector2,Vector2> ClosestPointResult;
        public Bounds bounds => new Bounds {center=Owner==null ? new Vector3() : Owner.transform.position,
            extents=new Vector3(Radius,Radius,0)};
        public Vector2 ClosestPoint(Vector2 point) => ClosestPointResult==null ? point : ClosestPointResult(point);
    }
    public struct LayerMask
    {
        public int value;
        public static implicit operator LayerMask(int value) => new LayerMask {value=value};
    }
    public struct ContactFilter2D
    {
        public bool useTriggers;
        public LayerMask Layers;
        public void SetLayerMask(LayerMask mask) { Layers=mask; }
    }
    public struct RaycastHit2D { public Collider2D collider;public Vector2 normal;public float distance; }
    public static class Physics2D
    {
        public static Func<Vector2,Vector2,float,RaycastHit2D[]> RayResults,CastResults;
        public static Func<Vector2,bool> PointBlocked;
        public static int Rays,Casts,OverlapQueries,LastMask;
        public static float LastCastDistance;
        public static Vector2 LastCastOrigin;
        public static int Raycast(Vector2 origin,Vector2 direction,ContactFilter2D filter,
            RaycastHit2D[] hits,float distance)
        { Rays++;LastMask=filter.Layers.value;return Copy(RayResults?.Invoke(origin,direction,distance),hits); }
        public static int CircleCast(Vector2 origin,float radius,Vector2 direction,ContactFilter2D filter,
            RaycastHit2D[] hits,float distance)
        { Casts++;LastMask=filter.Layers.value;LastCastDistance=distance;LastCastOrigin=origin;return Copy(CastResults?.Invoke(origin,direction,distance),hits); }
        public static int OverlapCircle(Vector2 point,float radius,ContactFilter2D filter,Collider2D[] results)
        { OverlapQueries++;LastMask=filter.Layers.value;return PointBlocked!=null && PointBlocked(point) ? 1 : 0; }
        private static int Copy(RaycastHit2D[] source,RaycastHit2D[] target)
        { if(source==null)return 0;int count=Math.Min(source.Length,target.Length);Array.Copy(source,target,count);return count; }
        public static void Reset()
        { RayResults=null;CastResults=null;PointBlocked=null;Rays=Casts=OverlapQueries=LastMask=0;LastCastDistance=0; }
    }
    public static class Mathf
    {
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Abs(float v)=>Math.Abs(v);
        public static float Sign(float v)=>v>=0?1:-1;
        public static float Clamp01(float v)=>Math.Max(0,Math.Min(1,v));
    }
    public static class Time { public static float deltaTime=0.1f,time; }
    public static class Random
    {
        private static System.Random random=new System.Random(1);
        public static void Reset()=>random=new System.Random(1);
        public static float Range(float low,float high)=>(float)(low+(high-low)*random.NextDouble());
    }
    public struct Color { public static Color yellow=>new Color();public static Color cyan=>new Color();public static Color green=>new Color(); }
    public static class Debug { public static void DrawRay(Vector2 point,Vector2 direction,Color color) { } }
    public static class Gizmos
    {
        public static Color color;
        public static void DrawWireSphere(Vector3 point,float radius) { }
        public static void DrawLine(Vector3 a,Vector3 b) { }
    }
}
namespace EscapeLab2D
{
    public class GameManager { public bool Playing=true; }
}

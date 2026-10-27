using System;
using System.Collections;
using System.Reflection;
using EscapeLab2D;
using UnityEngine;

internal static class Program
{
    private static int passed;

    private static void Main()
    {
        Check("01: seek points toward the target at the requested speed", () =>
            Near(new Vector2(3,4), SteeringMath.SeekVelocity(Vector2.zero,new Vector2(3,4),5)));
        Check("01: reaching the target or setting zero speed is safe", () =>
        {
            Near(Vector2.zero,SteeringMath.SeekVelocity(Vector2.zero,Vector2.zero,5));
            Near(Vector2.zero,SteeringMath.SeekVelocity(Vector2.zero,Vector2.right,-2));
        });
        Check("01: BasicSeek moves directly on its first frame", () =>
        {
            var h=Hunter(HunterBehaviour.BasicSeek);Set(h,"player",Point(10,0));Step(h);
            Near(new Vector2(0.5f,0),h.transform.position);Near(5,h.Velocity.magnitude);
        });
        Check("04: arrival uses full speed far away and slower speed nearby", () =>
        {
            float far=SteeringMath.ArrivalVelocity(Vector2.zero,new Vector2(10,0),5,2,0.15f).magnitude;
            float near=SteeringMath.ArrivalVelocity(Vector2.zero,new Vector2(1,0),5,2,0.15f).magnitude;
            Near(5,far);True(near>0 && near<far);
        });
        Check("04: arrival stops within its target radius without invalid values", () =>
        {
            Near(Vector2.zero,SteeringMath.ArrivalVelocity(Vector2.zero,new Vector2(0.1f,0),5,2,0.15f));
            Near(5,SteeringMath.ArrivalVelocity(Vector2.zero,new Vector2(1,0),5,0,0).magnitude);
        });
        Check("04: arrival simulation slows to a stop at a chosen point", () =>
        {
            var h=Hunter(HunterBehaviour.Arrival);Set(h,"arrivalTarget",Point(4,0));
            for(int i=0;i<600;i++)Step(h,1f/60f);
            True(Vector2.Distance(h.transform.position,new Vector2(4,0))<0.2f);
            True(h.Velocity.magnitude<0.01f);
        });
        Check("04: assigned arrival point takes precedence over the player", () =>
        {
            var h=Hunter(HunterBehaviour.Arrival);Set(h,"arrivalTarget",Point(1,0));
            Set(h,"player",Point(0,10));Step(h);True(h.Velocity.x>0);Near(0,h.Velocity.y);
        });
        Check("shared steering respects acceleration, speed and pause", () =>
        {
            Near(0.2f,SteeringMath.ApplySteering(Vector2.zero,new Vector2(10,0),2,5,0.1f).magnitude);
            Near(5,SteeringMath.ApplySteering(Vector2.zero,new Vector2(10,0),100,5,1).magnitude);
            Near(Vector2.right,SteeringMath.ApplySteering(Vector2.right,Vector2.left,10,5,0));
        });
        Check("07: wandering chooses a point inside its bounds and moves toward it", () =>
        {
            var h=Hunter(HunterBehaviour.WanderAndSeek);Set(h,"wanderBounds",new Rect(-2,-1,4,2));Step(h);
            True(h.HasWanderTarget);True(h.WanderTarget.x>=-2 && h.WanderTarget.x<=2);
            True(h.WanderTarget.y>=-1 && h.WanderTarget.y<=1);True(h.Velocity.magnitude>0);
        });
        Check("07: reaching a target selects another point", () =>
        {
            var h=Hunter(HunterBehaviour.WanderAndSeek);Set(h,"hasWanderTarget",true);
            Set(h,"wanderTarget",Vector2.zero);Set(h,"nextWanderTargetTime",100f);Step(h);
            True(h.HasWanderTarget);True(h.WanderTarget.magnitude>0.01f);
        });
        Check("07: points inside obstacles are rejected before choosing a free point", () =>
        {
            int rejected=0;Physics2D.PointBlocked=p=>++rejected<=2;
            var h=Hunter(HunterBehaviour.WanderAndSeek);Step(h);
            True(h.HasWanderTarget);Equal(3,Physics2D.OverlapQueries);
        });
        Check("07: fully blocked sampling is bounded and retries instead of looping forever", () =>
        {
            Physics2D.PointBlocked=p=>true;
            var h=Hunter(HunterBehaviour.WanderAndSeek);Step(h);
            True(!h.HasWanderTarget);Equal(24,Physics2D.OverlapQueries);Near(Vector2.zero,h.Velocity);
            Step(h,0.1f);Equal(24,Physics2D.OverlapQueries);
            Step(h,0.3f);Equal(48,Physics2D.OverlapQueries);
        });
        Check("07: stale targets get replaced after the configured timeout", () =>
        {
            var h=Hunter(HunterBehaviour.WanderAndSeek);Set(h,"wanderRetargetSeconds",0.5f);Step(h);
            Vector2 old=h.WanderTarget;Step(h,0.6f);
            True(Vector2.Distance(old,h.WanderTarget)>0.01f);
        });
        Check("10: one forward ray detects a wall and takes priority over seeking", () =>
        {
            var wall=new Collider2D();
            Physics2D.RayResults=(p,d,len)=>new[]{Hit(wall,0.5f,Vector2.left)};
            var h=Hunter(HunterBehaviour.BasicSeek);Set(h,"player",Point(10,0));Set(h,"obstacleAvoidance",true);Step(h);
            Equal(1,Physics2D.Rays);Equal(64,Physics2D.LastMask);
            True(Math.Abs(h.Velocity.y)>0.1f);True(h.Velocity.x<0);
        });
        Check("10: the closest wall wins even if query results are unsorted", () =>
        {
            var wall=new Collider2D();
            Physics2D.RayResults=(p,d,len)=>new[]{Hit(wall,1.5f,Vector2.left),Hit(wall,0.5f,Vector2.down)};
            var h=Hunter(HunterBehaviour.BasicSeek);Set(h,"player",Point(10,0));Set(h,"obstacleAvoidance",true);Step(h);
            True(h.Velocity.x>0);True(h.Velocity.y<0);
        });
        Check("10: the passive shape sweep prevents a step through a wall", () =>
        {
            Physics2D.CastResults=(p,d,len)=>new[]{Hit(new Collider2D(),0.1f,Vector2.left)};
            var h=Hunter(HunterBehaviour.BasicSeek);Set(h,"player",Point(10,0));Step(h);
            True(h.transform.position.x>0 && h.transform.position.x<0.11f);
        });
        Check("10: a zero-distance contact does not freeze parallel movement", () =>
        {
            var wall=new Collider2D {ClosestPointResult=p=>new Vector2(0,p.y)};
            Physics2D.CastResults=(p,d,len)=>new[]{Hit(wall,0,Vector2.down)};
            var h=Hunter(HunterBehaviour.BasicSeek);h.transform.position=new Vector2(1,0);
            Set(h,"player",Point(1,10));Step(h);True(h.transform.position.y>0.4f);
        });
        Check("10: high-speed movement sweeps the whole frame distance", () =>
        {
            Physics2D.CastResults=(p,d,len)=>new[]{Hit(new Collider2D(),0.9f,Vector2.left)};
            var h=Hunter(HunterBehaviour.BasicSeek);Set(h,"maxSpeed",8f);Set(h,"player",Point(10,0));
            Step(h,0.25f);Near(2,Physics2D.LastCastDistance);True(h.transform.position.x<0.91f);
        });
        Check("13: patrol advances within a radius without waiting for an exact point", () =>
        {
            var h=Hunter(HunterBehaviour.SmoothPatrol);
            h.SetPatrolWaypoints(new[]{Point(0.4f,0),Point(4,0)});Step(h);
            Equal(1,h.CurrentWaypointIndex);True(h.Velocity.x>0);
        });
        Check("10: collision queries use the current scaled collider offset", () =>
        {
            var h=Hunter(HunterBehaviour.BasicSeek);h.transform.position=new Vector2(2,4);
            h.transform.localScale=new Vector3(2,3,1);
            h.GetComponent<Collider2D>().offset=new Vector2(0.1f,-0.2f);
            Set(h,"player",Point(20,4));Step(h);
            Near(new Vector2(2.2f,3.4f),Physics2D.LastCastOrigin);
            Near(new Vector2(2.5f,4),h.transform.position);
        });
        Check("13: patrol wraps around and skips missing waypoint entries", () =>
        {
            var h=Hunter(HunterBehaviour.SmoothPatrol);
            h.SetPatrolWaypoints(new[]{null,Point(0.2f,0),null,Point(4,0)});Step(h);
            Equal(3,h.CurrentWaypointIndex);
            h.transform.position=new Vector2(4,0);Step(h);Equal(1,h.CurrentWaypointIndex);
        });
        Check("13: empty or missing patrol points are safe", () =>
        {
            var h=Hunter(HunterBehaviour.SmoothPatrol);h.SetPatrolWaypoints(null);Step(h);
            Near(Vector2.zero,h.Velocity);h.SetPatrolWaypoints(new Transform[]{null,null});Step(h);
            Near(Vector2.zero,h.Velocity);
        });
        Check("13: a single waypoint uses arrival rather than perpetual circling", () =>
        {
            var h=Hunter(HunterBehaviour.SmoothPatrol);h.SetPatrolWaypoints(new[]{Point(3,0)});
            for(int i=0;i<600;i++)Step(h,1f/60f);
            True(Vector2.Distance(h.transform.position,new Vector2(3,0))<0.2f);
            True(h.Velocity.magnitude<0.01f);
        });
        Check("13: multiple patrol laps keep moving and visiting waypoints", () =>
        {
            var h=Hunter(HunterBehaviour.SmoothPatrol);Set(h,"maxAcceleration",24f);
            h.SetPatrolWaypoints(new[]{Point(0,0),Point(6,0),Point(6,5),Point(0,5)});
            int changes=0,last=0;
            for(int i=0;i<1800;i++)
            {
                Step(h,1f/60f);if(h.CurrentWaypointIndex!=last){changes++;last=h.CurrentWaypointIndex;}
            }
            True(changes>=8);True(h.Velocity.magnitude>0.1f);
        });
        Check("16: only neighbours inside the separation radius contribute", () =>
        {
            Near(Vector2.zero,SteeringMath.SeparationOffset(Vector2.zero,new Vector2(4,0),3,1,2));
            Near(Vector2.zero,SteeringMath.SeparationOffset(Vector2.zero,Vector2.zero,3,1,1));
            True(SteeringMath.SeparationOffset(Vector2.zero,Vector2.right,3,1,2).x<0);
        });
        Check("16: closer hunters repel more strongly", () =>
        {
            float near=SteeringMath.SeparationOffset(Vector2.zero,new Vector2(0.2f,0),3,1,2).magnitude;
            float far=SteeringMath.SeparationOffset(Vector2.zero,new Vector2(2,0),3,1,2).magnitude;
            True(near>far);
        });
        Check("16: exact overlaps produce opposite finite directions", () =>
        {
            Vector2 a=SteeringMath.SeparationOffset(Vector2.zero,Vector2.zero,3,10,20);
            Vector2 b=SteeringMath.SeparationOffset(Vector2.zero,Vector2.zero,3,20,10);
            Near(Vector2.zero,a+b);True(a.magnitude>0);
        });
        Check("16: two coincident active hunters separate in the controller", () =>
        {
            var a=Hunter(HunterBehaviour.BasicSeek);var b=Hunter(HunterBehaviour.BasicSeek);
            Set(a,"player",Point(10,0));Set(b,"player",Point(10,0));
            Set(a,"separation",true);Set(b,"separation",true);Step(a);Step(b);
            True(a.transform.position.x<0);True(b.transform.position.x>0);
        });
        Check("16: disabling a hunter removes it from neighbour calculations", () =>
        {
            var a=Hunter(HunterBehaviour.BasicSeek);var b=Hunter(HunterBehaviour.BasicSeek);
            Set(a,"player",Point(10,0));Set(a,"separation",true);Call(b,"OnDisable");Step(a);
            True(a.Velocity.x>0);
        });
        Check("the Level 3 paused/finished state stops steering and target changes", () =>
        {
            TestScene.Manager=new GameManager {Playing=false};
            var h=Hunter(HunterBehaviour.WanderAndSeek);Step(h);
            Near(Vector2.zero,h.transform.position);True(!h.HasWanderTarget);
            Equal(0,Physics2D.Rays);TestScene.Manager.Playing=true;Step(h);
            True(h.HasWanderTarget);
        });
        Check("the inherited near-player response is retained in ExistingDetection mode", () =>
        {
            var h=Hunter(HunterBehaviour.ExistingDetection);Set(h,"player",Point(1,0));
            Set(h,"arrivalTarget",Point(0,10));Step(h);True(h.Velocity.x>0);Near(0,h.Velocity.y);
        });
        Console.WriteLine($"{passed} steering checks passed. Real Unity rendering and physics still need Play-mode checks.");
    }

    private static HunterMovement Hunter(HunterBehaviour mode)
    {
        var h=new HunterMovement();h.gameObject.AddComponent<Collider2D>();
        Set(h,"behaviour",mode);Set(h,"separation",false);Set(h,"obstacleAvoidance",false);
        Call(h,"Awake");Call(h,"OnEnable");return h;
    }
    private static Transform Point(float x,float y)=>new Transform {position=new Vector2(x,y)};
    private static RaycastHit2D Hit(Collider2D collider,float distance,Vector2 normal)
        =>new RaycastHit2D {collider=collider,distance=distance,normal=normal};
    private static void Step(HunterMovement h,float seconds=0.1f)
    { Time.deltaTime=seconds;Time.time+=seconds;Call(h,"Update"); }
    private static void Call(object target,string name)
    { target.GetType().GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(target,null); }
    private static void Set(object target,string name,object value)
    { target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value); }
    private static void Check(string name,Action test)
    {
        ((IList)typeof(HunterMovement).GetField("hunters",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null)).Clear();
        Physics2D.Reset();UnityEngine.Random.Reset();TestScene.Manager=null;Time.time=0;
        test();passed++;Console.WriteLine("PASS: "+name);
    }
    private static void True(bool value) { if(!value)throw new Exception("Expected true"); }
    private static void Equal<T>(T expected,T actual)
    { if(!object.Equals(expected,actual))throw new Exception($"Expected {expected}, got {actual}"); }
    private static void Near(float expected,float actual)
    { if(Math.Abs(expected-actual)>0.002f)throw new Exception($"Expected {expected}, got {actual}"); }
    private static void Near(Vector2 expected,Vector2 actual)
    { if(Vector2.Distance(expected,actual)>0.002f)throw new Exception("Unexpected vector"); }
}

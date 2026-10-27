using UnityEngine;

namespace EscapeLab2D
{
    /// <summary>Small, testable calculations shared by Thomas's steering tasks.</summary>
    public static class SteeringMath
    {
        // Task 01: directly seek a target at full speed.
        public static Vector2 SeekVelocity(Vector2 position, Vector2 target, float maxSpeed)
        {
            return (target - position).normalized * Mathf.Max(0f, maxSpeed);
        }

        // Task 04: reduce desired speed inside the slowing radius, and stop close enough.
        public static Vector2 ArrivalVelocity(Vector2 position, Vector2 target, float maxSpeed,
            float slowingRadius, float stopRadius)
        {
            Vector2 offset = target - position;
            float distance = offset.magnitude;
            stopRadius = Mathf.Max(0f, stopRadius);
            if (distance <= stopRadius) return Vector2.zero;
            float outerRadius = Mathf.Max(slowingRadius, stopRadius + 0.001f);
            float speed = Mathf.Max(0f, maxSpeed) *
                Mathf.Clamp01((distance - stopRadius) / (outerRadius - stopRadius));
            return offset.normalized * speed;
        }

        // desired - current is the steering change. Limit it by acceleration * seconds.
        public static Vector2 ApplySteering(Vector2 current, Vector2 desired, float acceleration,
            float maxSpeed, float deltaTime)
        {
            if (deltaTime <= 0f) return current;
            Vector2 steering = desired - current;
            Vector2 change = Vector2.ClampMagnitude(steering, Mathf.Max(0f, acceleration) * deltaTime);
            return Vector2.ClampMagnitude(current + change, Mathf.Max(0f, maxSpeed));
        }

        // Task 13: advance while inside this radius, rather than requiring an exact position.
        public static bool WithinRadius(Vector2 position, Vector2 target, float radius)
        {
            radius = Mathf.Max(0f, radius);
            return (target - position).sqrMagnitude <= radius * radius;
        }

        // Task 16: closer neighbours exert stronger repulsion. IDs break an exact overlap.
        public static Vector2 SeparationOffset(Vector2 position, Vector2 neighbour, float radius,
            int selfId, int neighbourId)
        {
            if (radius <= 0f || selfId == neighbourId) return Vector2.zero;
            Vector2 away = position - neighbour;
            float distance = away.magnitude;
            if (distance >= radius) return Vector2.zero;
            if (distance <= 0.0001f) return selfId < neighbourId ? Vector2.left : Vector2.right;
            return away / distance * (1f - distance / radius);
        }

        // Task 10: travel along a detected wall and gently away from its surface.
        public static Vector2 AvoidanceVelocity(Vector2 normal, float side, float maxSpeed)
        {
            normal = normal.normalized;
            Vector2 tangent = new Vector2(-normal.y, normal.x) * (side < 0f ? -1f : 1f);
            return (tangent + normal * 0.4f).normalized * Mathf.Max(0f, maxSpeed);
        }
    }
}

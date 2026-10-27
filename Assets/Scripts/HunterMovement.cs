using System.Collections.Generic;
using EscapeLab2D;
using UnityEngine;

public enum HunterBehaviour { BasicSeek, Arrival, WanderAndSeek, SmoothPatrol, ExistingDetection }

[RequireComponent(typeof(Collider2D))]
public class HunterMovement : MonoBehaviour
{
    [Header("Choose the behaviour to demonstrate")]
    [SerializeField] private HunterBehaviour behaviour = HunterBehaviour.ExistingDetection;
    [SerializeField] private Transform player;
    [Min(0f)] [SerializeField] private float maxSpeed = 5f;
    [Min(0f)] [SerializeField] private float maxAcceleration = 12f;
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private bool patrolWhenIdle;

    [Header("04 - Arrival: assign a point, or use the player")]
    [SerializeField] private Transform arrivalTarget;
    [Min(0.1f)] [SerializeField] private float slowingRadius = 2f;
    [Min(0f)] [SerializeField] private float stopRadius = 0.15f;

    [Header("07 - Wander & Seek: world-space target area")]
    [SerializeField] private Rect wanderBounds = new Rect(-13f, -6f, 26f, 12f);
    [Min(0.1f)] [SerializeField] private float wanderTargetRadius = 0.6f;
    [Min(0.5f)] [SerializeField] private float wanderRetargetSeconds = 5f;

    [Header("10 - One forward ray (not left/right feelers)")]
    [SerializeField] private bool obstacleAvoidance = true;
    [SerializeField] private LayerMask obstacleLayers = 64; // Layer 6: Walls.
    [Min(0.1f)] [SerializeField] private float lookAhead = 1.5f;

    [Header("13 - Smooth Patrol: teammate's task 12 can supply this list")]
    [SerializeField] private Transform[] patrolWaypoints = new Transform[0];
    [Min(0.1f)] [SerializeField] private float waypointRadius = 1f;

    [Header("16 - Separation")]
    [SerializeField] private bool separation = true;
    [Min(0.1f)] [SerializeField] private float separationRadius = 3f;
    [Min(0f)] [SerializeField] private float separationWeight = 1.5f;

    [Header("Runtime status - watch during Play mode")]
    [SerializeField] private string movementStatus = "Ready";

    private const float WallClearance = 0.04f;
    private static readonly List<HunterMovement> hunters = new List<HunterMovement>();
    private static int nextHunterId;
    private int hunterId;
    private readonly RaycastHit2D[] wallHits = new RaycastHit2D[16];
    private readonly Collider2D[] overlaps = new Collider2D[8];
    private Collider2D hunterCollider;
    private ContactFilter2D obstacleFilter;
    private GameManager game;
    private Vector2 velocity;
    private Vector2 wanderTarget;
    private bool hasWanderTarget;
    private float nextWanderTargetTime;
    private int waypointIndex;
    private Collider2D avoidedWall;
    private Vector2 avoidanceNormal;
    private float avoidanceSide = 1f;
    private float avoidUntil;
    private float blockedSeconds;
    private Vector2 blockingNormals;
    private Vector2 lastBlockingNormal;
    private Vector2 recoveryDirection;
    private float recoveryUntil;

    public int CurrentWaypointIndex => waypointIndex;
    public Vector2 Velocity => velocity;
    public Vector2 WanderTarget => wanderTarget;
    public bool HasWanderTarget => hasWanderTarget;
    public string MovementStatus => movementStatus;

    private void Awake()
    {
        hunterCollider = GetComponent<Collider2D>();
        game = FindAnyObjectByType<GameManager>();
        ConfigureObstacleFilter();
    }

    private void OnEnable()
    {
        if (!hunters.Contains(this))
        {
            // Only a local ordering is needed to break overlaps and choose turns.
            hunterId = ++nextHunterId;
            hunters.Add(this);
        }
        movementStatus = "Ready";
    }

    private void OnDisable()
    {
        hunters.Remove(this);
        velocity = Vector2.zero;
        blockedSeconds = 0f;
        recoveryUntil = 0f;
        avoidUntil = 0f;
        avoidedWall = null;
        movementStatus = "Disabled";
    }

    private void ConfigureObstacleFilter()
    {
        obstacleFilter = new ContactFilter2D { useTriggers = false };
        obstacleFilter.SetLayerMask(obstacleLayers);
    }

    private float Radius => Mathf.Max(hunterCollider.bounds.extents.x, hunterCollider.bounds.extents.y);
    // Use the current Transform rather than a physics-step-old bounds centre.
    private Vector2 Centre => transform.TransformPoint(hunterCollider.offset);

    private void Update()
    {
        if (Time.deltaTime <= 0f || (game != null && !game.Playing))
        {
            movementStatus = "Paused or round finished";
            return;
        }
        ConfigureObstacleFilter(); // Inspector layer changes also work during a demonstration.
        Vector2 desired = ChooseVelocity();

        if (Time.time < recoveryUntil)
        {
            movementStatus = "Recovering from wall";
            desired = recoveryDirection * maxSpeed;
        }
        else
        {
            // Task 16 adds only local repulsion; it does not implement Surround (task 17).
            if (separation)
                desired = Vector2.ClampMagnitude(desired + SeparationVelocity() * separationWeight, maxSpeed);
            // Task 10 takes priority when the single forward ray sees a wall.
            if (obstacleAvoidance) desired = AvoidObstacles(desired);
        }
        velocity = behaviour == HunterBehaviour.BasicSeek
            ? Vector2.ClampMagnitude(desired, maxSpeed)
            : SteeringMath.ApplySteering(velocity, desired, maxAcceleration, maxSpeed, Time.deltaTime);

        Vector2 displacement = MoveWithoutCrossingWalls(velocity * Time.deltaTime);
        transform.position += (Vector3)displacement;
        velocity = displacement / Time.deltaTime;
        if (desired.sqrMagnitude > 0.01f && displacement.sqrMagnitude < 0.000001f)
        {
            blockedSeconds += Time.deltaTime;
            movementStatus = "Blocked on wall";
            if (obstacleAvoidance && blockedSeconds >= 0.4f && blockingNormals.sqrMagnitude > 0.001f)
            {
                // Back out of a blocked corner, trying the opposite wall-following side.
                avoidanceSide = -avoidanceSide;
                Vector2 tangent = new Vector2(-lastBlockingNormal.y, lastBlockingNormal.x) * avoidanceSide;
                recoveryDirection = (blockingNormals.normalized + tangent * 0.5f).normalized;
                recoveryUntil = Time.time + 0.65f;
                avoidedWall = null;
                hasWanderTarget = false;
                nextWanderTargetTime = 0f;
                blockedSeconds = 0f;
                movementStatus = "Recovering from wall";
            }
        }
        else blockedSeconds = 0f;
    }

    private Vector2 ChooseVelocity()
    {
        Vector2 position = transform.position;
        movementStatus = behaviour.ToString();
        switch (behaviour)
        {
            case HunterBehaviour.BasicSeek: // 01: immediate, direct movement.
                return player == null ? Vector2.zero :
                    SteeringMath.SeekVelocity(position, player.position, maxSpeed);
            case HunterBehaviour.Arrival: // 04: slow down toward the chosen point.
                Transform target = arrivalTarget != null ? arrivalTarget : player;
                return target == null ? Vector2.zero : ArriveAt(target.position);
            case HunterBehaviour.SmoothPatrol: // 13: radius-based waypoint changes.
                return PatrolVelocity();
            case HunterBehaviour.ExistingDetection:
                // Reuse the existing near-player switch for ordinary gameplay.
                if (player != null && Vector2.Distance(position, player.position) < detectionRadius)
                {
                    movementStatus = "Chasing player";
                    return SteeringMath.SeekVelocity(position, player.position, maxSpeed);
                }
                if (patrolWhenIdle)
                {
                    movementStatus = "Patrolling while player is far away";
                    return PatrolVelocity();
                }
                movementStatus = "Wandering while player is far away";
                return WanderVelocity();
            default: // 07: choose random points and seek each in turn.
                return WanderVelocity();
        }
    }

    private Vector2 ArriveAt(Vector2 target)
    {
        return SteeringMath.ArrivalVelocity(transform.position, target, maxSpeed, slowingRadius, stopRadius);
    }

    private Vector2 WanderVelocity()
    {
        if (!hasWanderTarget && Time.time < nextWanderTargetTime) return Vector2.zero;
        if (!hasWanderTarget || Time.time >= nextWanderTargetTime ||
            SteeringMath.WithinRadius(transform.position, wanderTarget, wanderTargetRadius))
            ChooseWanderTarget();
        return hasWanderTarget
            ? SteeringMath.SeekVelocity(transform.position, wanderTarget, maxSpeed) : Vector2.zero;
    }

    private void ChooseWanderTarget()
    {
        hasWanderTarget = false;
        // Reject points inside walls. This is local steering, not A* route planning.
        for (int attempt = 0; attempt < 24; attempt++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(wanderBounds.xMin, wanderBounds.xMax),
                Random.Range(wanderBounds.yMin, wanderBounds.yMax));
            if (Physics2D.OverlapCircle(candidate, Radius + WallClearance, obstacleFilter, overlaps) != 0)
                continue;
            wanderTarget = candidate;
            hasWanderTarget = true;
            break;
        }
        // Reselect unreachable/stalled targets; do not keep pushing toward one forever.
        nextWanderTargetTime = Time.time + (hasWanderTarget ? wanderRetargetSeconds : 0.25f);
        if (!hasWanderTarget) movementStatus = "No free wander target - retrying";
    }

    public void SetPatrolWaypoints(Transform[] waypoints)
    {
        patrolWaypoints = waypoints ?? new Transform[0];
        waypointIndex = 0;
    }

    private Vector2 PatrolVelocity()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0) return Vector2.zero;
        waypointIndex %= patrolWaypoints.Length;
        int checkedPoints = 0;
        while (patrolWaypoints[waypointIndex] == null && checkedPoints++ < patrolWaypoints.Length)
            waypointIndex = (waypointIndex + 1) % patrolWaypoints.Length;
        if (patrolWaypoints[waypointIndex] == null) return Vector2.zero;

        Transform target = patrolWaypoints[waypointIndex];
        if (SteeringMath.WithinRadius(transform.position, target.position, waypointRadius))
        {
            int previous = waypointIndex;
            do { waypointIndex = (waypointIndex + 1) % patrolWaypoints.Length; }
            while (patrolWaypoints[waypointIndex] == null && waypointIndex != previous);
            // One valid waypoint uses arrival instead of circling endlessly.
            if (waypointIndex == previous) return ArriveAt(target.position);
            target = patrolWaypoints[waypointIndex];
        }
        // Do not stop at every point: advance early, retaining the current velocity.
        return SteeringMath.SeekVelocity(transform.position, target.position, maxSpeed);
    }

    private Vector2 SeparationVelocity()
    {
        Vector2 repulsion = Vector2.zero;
        foreach (HunterMovement other in hunters)
        {
            if (other == null || other == this) continue;
            repulsion += SteeringMath.SeparationOffset(transform.position, other.transform.position,
                separationRadius, hunterId, other.hunterId);
        }
        return Vector2.ClampMagnitude(repulsion, 1f) * maxSpeed;
    }

    private Vector2 AvoidObstacles(Vector2 desired)
    {
        Vector2 forward = velocity.sqrMagnitude > 0.01f ? velocity.normalized : desired.normalized;
        if (forward.sqrMagnitude <= 0f) return desired;
        Vector2 origin = Centre;
        float distance = Radius + lookAhead;
        Debug.DrawRay(origin, forward * distance, Color.yellow);
        int count = Physics2D.Raycast(origin, forward, obstacleFilter, wallHits, distance);
        if (ClosestHit(count, origin, forward, out RaycastHit2D hit))
        {
            if (avoidedWall != hit.collider)
            {
                Vector2 tangent = new Vector2(-hit.normal.y, hit.normal.x);
                float dot = Vector2.Dot(desired, tangent);
                avoidanceSide = Mathf.Abs(dot) > 0.01f ? Mathf.Sign(dot) :
                    (hunterId % 2 == 0 ? 1f : -1f);
            }
            avoidedWall = hit.collider;
            avoidanceNormal = hit.normal;
            avoidUntil = Time.time + 0.35f;
        }
        // Briefly keep the same turn, avoiding left/right flip-flopping at contact.
        if (avoidedWall != null && Time.time < avoidUntil)
        {
            movementStatus = "Avoiding wall";
            return SteeringMath.AvoidanceVelocity(avoidanceNormal, avoidanceSide, maxSpeed);
        }
        avoidedWall = null;
        return desired;
    }

    private bool ClosestHit(int count, Vector2 origin, Vector2 direction, out RaycastHit2D closest)
    {
        closest = default(RaycastHit2D);
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = wallHits[i];
            if (hit.collider == null || hit.collider == hunterCollider) continue;
            if (hit.distance <= 0.0001f)
            {
                Vector2 outward = origin - hit.collider.ClosestPoint(origin);
                bool insideWall = outward.sqrMagnitude <= 0.000001f;
                Vector2 normal = insideWall ? NearestBoundsExit(hit.collider.bounds, origin) : outward.normalized;
                // A cast starting at contact may report an artificial normal opposite
                // travel. Allow travel away from, or parallel to, the actual wall.
                float outwardMotion = Vector2.Dot(direction, normal);
                if ((!insideWall && outwardMotion >= -0.0001f) ||
                    (insideWall && outwardMotion > 0.0001f)) continue;
                // Preserve tangential movement when the query supplies -direction
                // for a starting overlap, instead of the actual wall normal.
                hit.normal = normal;
            }
            if (!found || hit.distance < closest.distance) { closest = hit; found = true; }
        }
        return found;
    }

    private static Vector2 NearestBoundsExit(Bounds bounds, Vector2 point)
    {
        // Recovery for an invalid start inside one of this level's box walls.
        float nearest = point.x - (bounds.center.x - bounds.extents.x);
        Vector2 normal = Vector2.left;
        float right = bounds.center.x + bounds.extents.x - point.x;
        if (right < nearest) { nearest = right; normal = Vector2.right; }
        float bottom = point.y - (bounds.center.y - bounds.extents.y);
        if (bottom < nearest) { nearest = bottom; normal = Vector2.down; }
        float top = bounds.center.y + bounds.extents.y - point.y;
        if (top < nearest) normal = Vector2.up;
        return normal;
    }

    private Vector2 MoveWithoutCrossingWalls(Vector2 displacement)
    {
        Vector2 origin = Centre;
        Vector2 position = origin;
        Vector2 remaining = displacement;
        blockingNormals = Vector2.zero;
        for (int i = 0; i < 2 && remaining.sqrMagnitude > 0.000001f; i++)
        {
            float distance = remaining.magnitude;
            Vector2 direction = remaining / distance;
            int count = Physics2D.CircleCast(position, Radius, direction, obstacleFilter, wallHits, distance);
            if (!ClosestHit(count, position, direction, out RaycastHit2D hit))
            {
                position += remaining;
                break;
            }
            Vector2 travelled = direction * Mathf.Max(0f, hit.distance - WallClearance);
            blockingNormals += hit.normal;
            lastBlockingNormal = hit.normal;
            position += travelled;
            remaining -= travelled;
            float intoWall = Vector2.Dot(remaining, hit.normal);
            if (intoWall < 0f) remaining -= intoWall * hit.normal;
        }
        return position - origin;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (hasWanderTarget) Gizmos.DrawWireSphere((Vector3)wanderTarget, wanderTargetRadius);
        if (arrivalTarget != null)
        {
            Gizmos.DrawWireSphere(arrivalTarget.position, slowingRadius);
            Gizmos.DrawWireSphere(arrivalTarget.position, stopRadius);
        }
        if (patrolWaypoints == null) return;
        Gizmos.color = Color.green;
        for (int i = 0; i < patrolWaypoints.Length; i++)
        {
            Transform point = patrolWaypoints[i];
            if (point == null) continue;
            Gizmos.DrawWireSphere(point.position, waypointRadius);
            Transform next = patrolWaypoints[(i + 1) % patrolWaypoints.Length];
            if (next != null) Gizmos.DrawLine(point.position, next.position);
        }
    }
}

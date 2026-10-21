using UnityEngine;

public class RivalAI : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 2f;
    [SerializeField] private float maxSteeringForce = 8f;
    [SerializeField] private float fleeRadius = 3f;
    [SerializeField] private float roamDirectionInterval = 2f;
    [SerializeField] private float hunterRefreshInterval = 0.5f;

    private GameObject[] hunters = System.Array.Empty<GameObject>();
    private Vector2 velocity;
    private Vector2 roamDirection;
    private float nextRoamDirectionTime;
    private float nextHunterRefreshTime;

    private void Start()
    {
        RefreshHunters();
        ChooseRoamDirection();
    }

    private void Update()
    {
        if (Time.time >= nextHunterRefreshTime)
        {
            RefreshHunters();
        }

        GameObject nearbyHunter = FindNearbyHunter();
        if (nearbyHunter != null)
        {
            Vector2 fleeDirection = (Vector2)transform.position - (Vector2)nearbyHunter.transform.position;
            if (fleeDirection.sqrMagnitude < 0.001f)
            {
                fleeDirection = Random.insideUnitCircle;
            }

            ApplySteering(fleeDirection.normalized * maxSpeed - velocity);
            return;
        }

        if (Time.time >= nextRoamDirectionTime)
        {
            ChooseRoamDirection();
        }

        ApplySteering(roamDirection * maxSpeed - velocity);
    }

    private GameObject FindNearbyHunter()
    {
        GameObject nearestHunter = null;
        float nearestDistanceSquared = fleeRadius * fleeRadius;

        foreach (GameObject hunter in hunters)
        {
            if (hunter == null)
            {
                continue;
            }

            float distanceSquared = ((Vector2)hunter.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (distanceSquared < nearestDistanceSquared)
            {
                nearestDistanceSquared = distanceSquared;
                nearestHunter = hunter;
            }
        }

        return nearestHunter;
    }

    private void ApplySteering(Vector2 steering)
    {
        steering = Vector2.ClampMagnitude(steering, maxSteeringForce);
        velocity = Vector2.ClampMagnitude(velocity + steering * Time.deltaTime, maxSpeed);
        transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    private void ChooseRoamDirection()
    {
        roamDirection = Random.insideUnitCircle.normalized;
        if (roamDirection.sqrMagnitude < 0.001f)
        {
            roamDirection = Vector2.right;
        }

        nextRoamDirectionTime = Time.time + Mathf.Max(0.1f, roamDirectionInterval);
    }

    private void RefreshHunters()
    {
        hunters = GameObject.FindGameObjectsWithTag("Hunter");
        nextHunterRefreshTime = Time.time + Mathf.Max(0.1f, hunterRefreshInterval);
    }
}

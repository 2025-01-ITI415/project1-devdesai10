using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Grappling : MonoBehaviour
{
    [Header("References")]
    public FirstPersonController fpsController; // Reference to the FPS controller
    public Transform cam; // Main camera
    public Transform gunTip; // Grappling gun tip
    public LayerMask whatIsGrappleable; // Layers that can be grappled
    public LineRenderer lr; // Line renderer for the grappling rope

    [Header("Grappling Settings")]
    public float maxGrappleDistance = 100f; // Maximum grapple distance
    public float grappleDelayTime = 0.5f; // Delay before grappling starts
    public float overshootYAxis = 2f; // How high the player overshoots the grapple point

    [Header("Cooldown")]
    public float grapplingCd = 2f; // Cooldown between grapples
    private float grapplingCdTimer; // Timer for cooldown

    [Header("Input")]
    public KeyCode grappleKey = KeyCode.Mouse1; // Grapple input key

    private Vector3 grapplePoint; // Point where the grapple connects
    private bool grappling; // Whether the player is currently grappling

    private void Start()
    {
        lr.enabled = false; // Disable the line renderer initially
    }

    private void Update()
    {
        // Check for grapple input
        if (Input.GetKeyDown(grappleKey)) StartGrapple();

        // Update cooldown timer
        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        // Update the grappling rope's position during grappling
        if (grappling)
        {
            lr.SetPosition(0, gunTip.position); // Start of the rope at the gun tip
            lr.SetPosition(1, grapplePoint);   // End of the rope at the grapple point
        }
    }

    private void StartGrapple()
    {
        // Check if grappling is on cooldown
        if (grapplingCdTimer > 0) return;

        grappling = true;
        fpsController.freezeMovement = true; // Freeze FPS controller movement

        // Perform a raycast to find the grapple point
        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point; // Set the grapple point
            Invoke(nameof(ExecuteGrapple), grappleDelayTime); // Start grappling after delay
        }
        else
        {
            // If no grapple point is found, stop grappling
            grapplePoint = cam.position + cam.forward * maxGrappleDistance;
            Invoke(nameof(StopGrapple), grappleDelayTime);
        }

        lr.enabled = true; // Enable the line renderer
        lr.SetPosition(1, grapplePoint); // Set the end position of the rope
    }

    private void ExecuteGrapple()
    {
        fpsController.freezeMovement = false; // Unfreeze movement

        // Calculate the lowest point of the player
        Vector3 lowestPoint = new Vector3(transform.position.x, transform.position.y - 1f, transform.position.z);

        // Calculate the relative Y position of the grapple point
        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOnArc = grapplePointRelativeYPos + overshootYAxis;

        // If the grapple point is below the player, adjust the arc
        if (grapplePointRelativeYPos < 0) highestPointOnArc = overshootYAxis;

        // Move the player to the grapple point
        StartCoroutine(JumpToPosition(grapplePoint, highestPointOnArc));

        Invoke(nameof(StopGrapple), 1f); // Stop grappling after 1 second
    }

    private IEnumerator JumpToPosition(Vector3 targetPosition, float trajectoryHeight)
    {
        float time = 0;
        Vector3 startPosition = transform.position;

        while (time < 1)
        {
            time += Time.deltaTime / grappleDelayTime; // Normalized time
            float height = Mathf.Sin(time * Mathf.PI) * trajectoryHeight; // Calculate arc height

            // Move the player along the arc
            transform.position = Vector3.Lerp(startPosition, targetPosition, time) + Vector3.up * height;
            yield return null;
        }

        transform.position = targetPosition; // Ensure the player reaches the target position
    }

    public void StopGrapple()
    {
        fpsController.freezeMovement = false; // Unfreeze movement
        grappling = false; // Stop grappling
        grapplingCdTimer = grapplingCd; // Start cooldown
        lr.enabled = false; // Disable the line renderer
    }

    public bool IsGrappling()
    {
        return grappling;
    }

    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
    }
}
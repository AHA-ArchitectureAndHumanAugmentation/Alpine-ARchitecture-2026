using UnityEngine;

public class AddForce : MonoBehaviour
{
    public Rigidbody Rb;
    public bool addForce;
    public Vector3 Force;
    public Vector3 Velocity;
    public bool ResetVelocity;

    // Physics steps need to be in FixedUpdate instead of update
    private void FixedUpdate()
    {
        if (addForce)
        {
            Rb.AddForce(Force, ForceMode.Impulse);
            addForce = false; // de-activate the field so i can do it again 
        }
    }

    private void Update()
    {
        if (ResetVelocity)
        {
            // Try writing Vector3. and see what else Unity provides for you 
            Rb.linearVelocity = Vector3.zero;
        }

        // This is just to show us the velocity in the inspector. We don't need/use this variable
        Velocity = Rb.linearVelocity;
    }
}

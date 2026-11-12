using UnityEngine;

public class AddForce : MonoBehaviour
{
    public Rigidbody Rb;
    public bool addForce;
    public Vector3 Force;
    public Vector3 Velocity;
    public bool ResetVelocity;

    private void FixedUpdate()
    {
        if (addForce)
        {
            Rb.AddForce(Force, ForceMode.Impulse);
            addForce = false;
        }
    }

    private void Update()
    {
        if (ResetVelocity)
        {
            Rb.linearVelocity = Vector3.zero;
        }

        Velocity = Rb.linearVelocity;
    }
}

using UnityEngine;

namespace PortBoss
{
    // Tilts the old pier end to the host's sag angle (PortGame.PierSag), smoothly, as a kinematic body so
    // crates and players on it ride the tilt (and slide off when it gets steep).
    public class PierSag : MonoBehaviour
    {
        Rigidbody rb;
        float angle;
        Quaternion rest;
        int creakStep;

        void Awake() { rb = GetComponent<Rigidbody>(); rest = transform.rotation; }

        void FixedUpdate()
        {
            float target = PortGame.I != null ? PortGame.I.PierSag.Value : 0f;
            angle = Mathf.MoveTowards(angle, target, Time.fixedDeltaTime * (target > angle ? 2.5f : 6f));
            rb.MoveRotation(rest * Quaternion.Euler(angle, 0, 0));
            int step = Mathf.FloorToInt(angle / 1.5f);
            if (step > creakStep) Sfx.Play("creak", transform.position + new Vector3(0, 0, 5), 1f, 0.7f);
            creakStep = step;
        }
    }
}

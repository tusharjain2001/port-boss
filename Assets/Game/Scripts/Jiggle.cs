using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // Spring jiggle (replan/feel §4, the R.E.P.O. floppy head): after the clips and the IK, a bone's tip is left behind
    // by sudden changes of speed and swings back on a damped spring, so the head nods into a stop and the hat wobbles on a
    // landing. It answers acceleration only (steady running or a boat under way leaves it upright), and stays within a
    // few degrees of the animated pose.
    [DefaultExecutionOrder(1050)] // after CharAnim's IK and gaze (1000), so the nod rides on top of where the eyes look
    public class Jiggle : MonoBehaviour
    {
        class B
        {
            public Transform T; public Vector3 Axis; public float Len, K, Zeta, Max; public Quaternion? Rest;
            public Vector3 O, OV, LastTip, LastV; public int Frames;
        }
        readonly List<B> bones = new();
        Animator an;

        public static Jiggle On(Animator an)
        {
            if (!an.TryGetComponent<Jiggle>(out var j)) j = an.gameObject.AddComponent<Jiggle>();
            j.an = an; return j;
        }

        // `up`: which way the bone's tip points now (the bind pose), `len` m to it; stiffness k (1/s²), damping ratio, limit.
        // `clipped`: the clips pose it every frame (a humanoid bone); otherwise (the hat) it is put back to rest first.
        public void Bone(Transform t, Vector3 up, float len, float k, float zeta, float maxDeg, bool clipped = true)
        {
            if (t != null) bones.Add(new B { T = t, Axis = t.InverseTransformDirection(up.normalized), Len = len, K = k, Zeta = zeta, Max = maxDeg, Rest = clipped ? null : t.localRotation });
        }

        void LateUpdate()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0) return;
            foreach (var b in bones)
            {
                if (b.Rest != null) b.T.localRotation = b.Rest.Value;
                var root = b.T.position; var tip = root + b.T.rotation * b.Axis * b.Len;
                // the ragdoll has the body, or a teleport (a ladder, a get-up): start again from rest
                if (!an.enabled || (tip - b.LastTip).sqrMagnitude > 0.25f) { b.Frames = 0; b.O = b.OV = Vector3.zero; b.LastTip = tip; continue; }
                var v = (tip - b.LastTip) / dt; b.LastTip = tip;
                var acc = b.Frames++ > 1 ? Vector3.ClampMagnitude((v - b.LastV) / dt, 60f) : Vector3.zero; b.LastV = v;
                // the tip's lag behind its animated place: o'' = -k o - c o' - a
                b.OV += (-b.K * b.O - 2 * b.Zeta * Mathf.Sqrt(b.K) * b.OV - acc) * dt; b.O += b.OV * dt;
                var q = Quaternion.FromToRotation(tip - root, tip + b.O - root);
                q.ToAngleAxis(out float ang, out var ax);
                if (ang > b.Max) q = Quaternion.AngleAxis(b.Max, ax);
                b.T.rotation = q * b.T.rotation;
            }
        }
    }
}

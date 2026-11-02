using System.Collections.Generic;
using UnityEngine;

namespace PortBoss.M1
{
    // Ragdoll-lite (replan/feel §4): rigidbodies, colliders and joints on a body's main bones, asleep (kinematic, colliders
    // off) until a knock: then the pose goes limp from wherever it was, flung the way it was hit. Back up, the animation
    // takes over again, blended in from the pose the body fell into. Built at runtime from the humanoid bones and their
    // lengths (sizes scaled by the body's height), so every kit body gets one.
    [DefaultExecutionOrder(100)] // LateUpdate after CharAnim's IK: the blend out of the ragdoll pose is the last word
    public class Ragdoll : MonoBehaviour
    {
        public bool Active { get; private set; }
        public Transform Hips, Chest, Head;
        public readonly List<Transform> Parts = new(); // the bones with a body, parents first (co-op sends their pose)
        readonly List<Rigidbody> bodies = new();
        readonly List<Collider> cols = new();
        Animator an;
        Transform[] bones; Quaternion[] snap; Vector3 snapHipsP; Quaternion snapHipsR;
        float blend = -1, blendLen = 0.3f;

        public static Ragdoll Build(Animator an, Collider ignore)
        {
            var r = an.gameObject.AddComponent<Ragdoll>(); r.an = an;
            Transform B(HumanBodyBones b) => an.GetBoneTransform(b);
            r.Hips = B(HumanBodyBones.Hips); r.Head = B(HumanBodyBones.Head);
            r.Chest = B(HumanBodyBones.UpperChest) ?? B(HumanBodyBones.Chest) ?? B(HumanBodyBones.Spine);
            r.bones = r.Hips.GetComponentsInChildren<Transform>(); r.snap = new Quaternion[r.bones.Length];
            // sizes from the bind pose: s = 1 for Pip (head joint 1.2 m up)
            Transform lf = B(HumanBodyBones.LeftFoot), lua = B(HumanBodyBones.LeftUpperArm), rua = B(HumanBodyBones.RightUpperArm);
            float s = Mathf.Max(0.5f, (r.Head.position.y - lf.position.y) / 1.2f);
            float bodyR = Mathf.Max(0.1f, Vector3.Distance(lua.position, rua.position) * 0.48f);
            var fwd = Vector3.ProjectOnPlane(r.Chest.forward, Vector3.up).normalized;

            var hips = r.Part(r.Hips, null, 12, Capsule(r.Hips, r.Hips.position, r.Chest.position, bodyR), Vector3.zero, 0, 0, 0, 0);
            var chest = r.Part(r.Chest, hips, 9, Sphere(r.Chest, r.Chest.position + Vector3.up * 0.04f * s, bodyR * 0.95f), Vector3.up, -20, 20, 20, 15);
            r.Part(r.Head, chest, 5, Sphere(r.Head, r.Head.position + Vector3.up * 0.09f * s, 0.17f * s), Vector3.up, -40, 40, 35, 35);
            foreach (var (u, l, h, side) in new[] { (HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -1), (HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, 1) })
            {
                Transform tu = B(u), tl = B(l), th = B(h);
                var ua = r.Part(tu, chest, 2, Capsule(tu, tu.position, tl.position, 0.065f * s), tl.position - tu.position, -60, 60, 85, 45);
                // the elbow bends forward only: a hinge about (upper arm x forward), 0..120°
                var hand = th.position + (th.position - tl.position).normalized * 0.07f * s;
                r.Part(tl, ua, 1.5f, Capsule(tl, tl.position, hand, 0.06f * s), Vector3.Cross(tl.position - tu.position, fwd), -5, 120, 8, 8, tl.position - tu.position);
            }
            foreach (var (u, l, f) in new[] { (HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot), (HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot) })
            {
                Transform tu = B(u), tl = B(l), tf = B(f);
                var ul = r.Part(tu, hips, 3, Capsule(tu, tu.position, tl.position, 0.085f * s), tl.position - tu.position, -30, 30, 70, 35);
                // the knee bends backward only
                var foot = tf.position + Vector3.down * 0.05f * s;
                r.Part(tl, ul, 2.5f, Capsule(tl, tl.position, foot, 0.075f * s), Vector3.Cross(tl.position - tu.position, fwd), -120, 5, 8, 8, tl.position - tu.position);
            }
            foreach (var c in r.cols) { if (ignore != null) Physics.IgnoreCollision(c, ignore); c.enabled = false; }
            return r;
        }

        // A rigidbody on a bone (kinematic until a knock); `axis` = the joint's twist axis (world), `swingAxis` (world) a
        // second axis for hinges (the bone itself); none for the hips (the root of the ragdoll).
        Rigidbody Part(Transform t, Rigidbody parent, float mass, Collider c, Vector3 axis, float lo, float hi, float swing1, float swing2, Vector3 swingAxis = default)
        {
            var rb = t.gameObject.AddComponent<Rigidbody>();
            rb.mass = mass; rb.isKinematic = true; rb.linearDamping = 0.05f; rb.angularDamping = 0.6f;
            rb.interpolation = RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            bodies.Add(rb); cols.Add(c); Parts.Add(t);
            if (parent != null)
            {
                var j = t.gameObject.AddComponent<CharacterJoint>();
                j.connectedBody = parent; j.enableProjection = true; j.projectionDistance = 0.04f;
                j.axis = t.InverseTransformDirection(axis.normalized);
                var sw = swingAxis != default ? swingAxis : Vector3.Cross(axis, Vector3.Cross(Vector3.up, axis).sqrMagnitude > 1e-4f ? Vector3.up : Vector3.forward);
                j.swingAxis = t.InverseTransformDirection(sw.normalized);
                j.lowTwistLimit = new SoftJointLimit { limit = lo }; j.highTwistLimit = new SoftJointLimit { limit = hi };
                j.swing1Limit = new SoftJointLimit { limit = swing1 }; j.swing2Limit = new SoftJointLimit { limit = swing2 };
            }
            return rb;
        }

        static Collider Capsule(Transform t, Vector3 a, Vector3 b, float r)
        {
            var c = t.gameObject.AddComponent<CapsuleCollider>();
            Vector3 la = t.InverseTransformPoint(a), lb = t.InverseTransformPoint(b), d = lb - la;
            int ax = Mathf.Abs(d.x) > Mathf.Abs(d.y) ? (Mathf.Abs(d.x) > Mathf.Abs(d.z) ? 0 : 2) : (Mathf.Abs(d.y) > Mathf.Abs(d.z) ? 1 : 2);
            float sc = t.lossyScale.x;
            c.direction = ax; c.center = (la + lb) * 0.5f; c.radius = r / sc; c.height = d.magnitude + 2 * r / sc;
            return c;
        }
        static Collider Sphere(Transform t, Vector3 at, float r)
        {
            var c = t.gameObject.AddComponent<SphereCollider>();
            c.center = t.InverseTransformPoint(at); c.radius = r / t.lossyScale.x;
            return c;
        }

        // Limp from the current pose, every part moving at `v` and the whole body tipping at `spin` (rad/s).
        public void Go(Vector3 v, Vector3 spin)
        {
            an.enabled = false; blend = -1;
            foreach (var c in cols) c.enabled = true;
            foreach (var rb in bodies) { rb.isKinematic = false; rb.linearVelocity = v + Vector3.Cross(spin, rb.worldCenterOfMass - Hips.position); rb.angularVelocity = spin; }
            Active = true;
        }

        // Co-op: a crewmate's body lying where their own ragdoll lies (their machine sends the parts' pose), no physics here.
        public void Puppet()
        {
            an.enabled = false; blend = -1; Active = true;
            foreach (var rb in bodies) rb.interpolation = RigidbodyInterpolation.None; // (kinematic: the pose is set, not simulated)
        }
        public void SetPose(Vector3 hips, Quaternion[] rot)
        {
            Hips.position = hips;
            for (int i = 0; i < Parts.Count && i < rot.Length; i++) Parts[i].rotation = rot[i];
        }

        public float Speed { get { float s = 0; foreach (var rb in bodies) s = Mathf.Max(s, rb.linearVelocity.magnitude); return s; } }

        // Back to the animation: the pose it fell into is kept and blended out over `len` s (the body's root may be moved
        // in between: the hips keep their place in the world, every other bone its turn on its parent).
        public void Stop(float len)
        {
            if (!Active) return;
            for (int i = 0; i < bones.Length; i++) snap[i] = bones[i].localRotation;
            snapHipsP = Hips.position; snapHipsR = Hips.rotation;
            foreach (var rb in bodies) rb.isKinematic = true;
            foreach (var c in cols) c.enabled = false;
            an.enabled = true; Active = false; blend = 0; blendLen = len;
        }

        void LateUpdate()
        {
            if (blend < 0) return;
            blend += Time.deltaTime / blendLen;
            float w = Mathf.SmoothStep(0, 1, Mathf.Clamp01(blend));
            for (int i = 1; i < bones.Length; i++) bones[i].localRotation = Quaternion.Slerp(snap[i], bones[i].localRotation, w);
            Hips.SetPositionAndRotation(Vector3.Lerp(snapHipsP, Hips.position, w), Quaternion.Slerp(snapHipsR, Hips.rotation, w));
            if (blend >= 1) blend = -1;
        }
    }
}

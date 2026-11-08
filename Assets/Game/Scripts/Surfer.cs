using UnityEngine;

namespace PortBoss.M1
{
    // M8 step 6: surfing (systems §1.7), on a board you own (Surf.cs), on the surf (SurfWaves.cs).
    // - In the water with a board: E lies you on it (the board isn't a hotbar slot: it comes out when you surf).
    // - Paddle: W paddles (the swim stroke, lying on the board), A/D turn, S slows. White water pushes you shoreward unless
    //   you duck-dive under it (Ctrl / pad B, held: you and the board go under). E gets off.
    // - Catch: on a wave's green face (not the white water), heading in and paddling, the pop-up bar: one stop (the fishing
    //   bar's look); Space / LMB / pad A as the needle crosses the green stands you up. A miss: the wave goes by. The
    //   bodyboard has no pop-up (you ride it lying down).
    // - Ride: A/D carve along the face. Down the face is speed (gravity on the slope), the pocket by the curl (a glowing
    //   band) is more speed, and the wave carries you. Too far round (over 125 degrees off the way in) or caught by the
    //   broken section: a wipeout (a tumble, bubbles, the leash brings the board back; then you're paddling again). Falling
    //   off the back of the wave or reaching the sand: the ride's over, you lie on the board again.
    // - The boards: the foamie is stable (a wide green, slow), the longboard smooth (a wide pocket), the shortboard fast and
    //   twitchy (a narrow green, quick turns), the bodyboard catches anything and rides the white water.
    // - Co-op: the rider's pose carries it (Angler.Pose State 7: Up = state | board << 4, UpT = the lean): crewmates see
    //   the board under them, paddling, popping, riding, wiping out.
    public class Surfer : MonoBehaviour
    {
        public enum St : byte { Off, Paddle, Pop, Ride, Wipe }
        public St State; public float StateT; public int Board;
        public bool Active => State != St.Off;                       // (mine)
        public bool Shown => netOn || Active;                        // (anyone's, for the pose)
        public string LastOutcome; public float LastRide;            // (tests)
        public static System.Action<string> OnLog;
        Angler me;

        // the board's tuning: paddle speed, top speed, pop green, turn rate (deg/s), the pocket's width (m)
        static readonly float[] PaddleV = { 0, 1.6f, 1.8f, 1.8f, 1.4f }, VMax = { 0, 8f, 8.5f, 8.5f, 11f }, Green = { 0, 0.34f, 1f, 0.30f, 0.16f }, Turn = { 0, 90, 110, 70, 140 }, PocketW = { 0, 6, 7, 9, 5 };
        static readonly string[] Model = { "", "foam", "body", "long", "short" };

        // the board in the world
        public Vector3 Pos; public float Yaw, Speed, Lean; float duckT = -1, duckCool;
        // riding: the wave (its id), where on it (u along the crest, x from the crest toward the shore), the heading off the
        // way in (theta, degrees: + to the wave's right), the speed along the heading
        public int WaveId = -1; public float U, X, Theta, PopNeedle; float popGreen; bool popRight;
        public bool InPocket;
        Transform board; LineRenderer band; RectTransform popUi, popZone, popPtr;
        bool netOn; byte netUp; float netT; int netWave;
        public Vector3 NetWave => new(WaveId, U, X);                  // (the pose's Hips while surfing: the wave I'm on)

        void Awake() { me = GetComponent<Angler>(); }
        static void Log(string s) { Debug.Log("[PB] surf: " + s); OnLog?.Invoke(s); }

        // ---- E in the water: on the board
        public static string Prompt(Angler me)
        {
            if (me.Remote || me.Surf == null || me.Surf.Active || Firm.I == null) return null;
            int b = Firm.I.MyKit.Board; if (!SurfGear.Owns(Firm.I.MyKit, b)) return null;
            if (Ctl.Interact) me.Surf.Begin(b);
            return $"Lie on your {SurfGear.Boards[b].Name.ToLowerInvariant()}";
        }

        public void Begin(int b)
        {
            Board = b; Pos = transform.position; Pos.y = Sea.Height(Pos); Yaw = transform.eulerAngles.y; Speed = 0; Lean = 0; WaveId = -1;
            me.Fish.Stow(); me.Cc.enabled = false; Go(St.Paddle); ShowBoard(b);
            Log($"on the {SurfGear.Boards[b].Name} at {Pos.x:F0} {Pos.z:F0}");
        }
        public void End()
        {
            if (State == St.Off) return;
            Go(St.Off); ShowBoard(0); Pop(false); Band(false);
            var p = transform.position; p.y = Sea.Height(p) - Angler.SwimDepth;
            me.Teleport(p, Yaw); me.Cc.enabled = true;
            Log("off the board");
        }
        void Go(St s) { State = s; StateT = 0; }

        // ---- the local rider, from the Angler's Update (instead of walking / swimming)
        public void Tick(float dt)
        {
            StateT += dt;
            switch (State)
            {
                case St.Paddle: Paddle(dt); break;
                case St.Pop: Popping(dt); break;
                case St.Ride: Ride(dt); break;
                case St.Wipe: Wiping(dt); break;
            }
            Place();
        }

        void Paddle(float dt)
        {
            if (Ctl.Interact) { End(); return; }
            var mv = Ctl.Move; float steer = Ctl.Steer;
            Yaw += steer * 80f * dt;
            float want = mv.y > 0.2f ? PaddleV[Board] : mv.y < -0.2f ? -0.4f : 0;
            Speed = Mathf.MoveTowards(Speed, want, dt * (want > Speed ? 1.6f : 2.5f));
            var fwd = Quaternion.Euler(0, Yaw, 0) * Vector3.forward;
            Pos += fwd * Speed * dt;
            // the duck-dive: held, up to 1.3 s; then 0.6 s before another
            duckCool -= dt;
            if (Duck && duckT < 0 && duckCool <= 0) { duckT = 0; Snd.Play(Synth.Plunk, Pos, 0.5f, 0.8f); }
            if (duckT >= 0) { duckT += dt; if (!Duck || duckT > 1.3f) { duckT = -1; duckCool = 0.6f; Fx.Splash(Pos, 0.4f, 6); } }
            // white water: pushed in, unless under it
            SurfWaves.Bump(Pos.x, Pos.z, out float foam);
            var sp = SurfWaves.Live;
            if (sp != null && foam > 0.45f && duckT < 0 && Board != 2)
            {
                Pos += new Vector3(sp.Dir.x, 0, sp.Dir.y) * 3.2f * dt; Speed *= 1 - dt * 2;
                if (Random.value < dt * 6) Fx.Splash(Pos, 0.5f, 5);
            }
            // a green face under me, heading in, paddling: catch it
            if (sp != null && mv.y > 0.2f && Speed > 0.8f && duckT < 0)
            {
                var din = new Vector3(sp.Dir.x, 0, sp.Dir.y);
                if (Vector3.Dot(fwd, din) > 0.3f)
                    for (int k = 0; k < SurfWaves.N; k++)
                    {
                        var w = SurfWaves.W[k]; if (w.H < 0.4f || w.S < -22f) continue;          // (out past 22 m from its break it's still a gentle swell)
                        SurfWaves.Frame(Pos, out float u, out float s); float x = s - w.S;
                        SurfWaves.Local(k, u, x, out float bs, out _, out float wface);
                        if (x < 0.05f || x > wface * 0.9f || bs > 0.3f) continue;
                        WaveId = w.Id; U = u; X = x; popRight = u > w.U0;
                        if (Board == 2) { StartRide(); return; }
                        popGreen = Green[Board] * (Settings.EasyFishing ? 1.3f : 1f); PopNeedle = 0; Go(St.Pop); Pop(true);
                        Log($"a wave ({w.H:F1} m): pop-up"); return;
                    }
            }
        }
        public static bool Duck => Ctl.Bot ? BotDuck : Ctl.Ui == 0 && (UnityEngine.InputSystem.Keyboard.current?.leftCtrlKey.isPressed == true || UnityEngine.InputSystem.Gamepad.current?.buttonEast.isPressed == true);
        public static bool BotDuck;

        // the pop-up: the needle crosses the bar once in 1.1 s while the wave lifts the board; the green near the end
        void Popping(float dt)
        {
            int k = SurfWaves.Find(WaveId); if (k < 0) { Missed("the wave's gone"); return; }
            var w = SurfWaves.W[k]; var sp = SurfWaves.Live;
            Pos = WavePoint(sp, w, U, X);                               // carried on the face as it lifts (X holds: it moves with the crest)
            PopNeedle = Mathf.Clamp01(StateT / 1.1f);
            float c0 = 0.62f; bool inGreen = Mathf.Abs(PopNeedle - c0) <= popGreen / 2;
            PopShow(c0);
            if (Ctl.Strike || Ctl.Bot && Bot.PopNow) { if (inGreen) StartRide(); else Missed("too early"); return; }
            if (PopNeedle >= 1) Missed("too late");
        }
        public static class Bot { public static bool PopNow; }
        void Missed(string why)
        {
            Pop(false); WaveId = -1; Speed = 0.6f; Go(St.Paddle); LastOutcome = "missed";
            Alerts.Push("Missed it", why == "too early" ? "Too early on the pop-up. The next one." : why == "too late" ? "Too late: it went by. The next one." : "It went by.", Alerts.Kind.Info);
            Log("missed: " + why);
        }

        void StartRide()
        {
            Pop(false); int k = SurfWaves.Find(WaveId); var w = SurfWaves.W[k];
            Theta = (popRight ? 1 : -1) * 35f; Speed = 4.5f; Go(St.Ride); rideT = 0; Snd.Play(Synth.Whoosh, Pos, 0.6f, 1.2f);
            Fx.Splash(Pos, 0.6f, 10); Log($"up and riding ({SurfGear.Boards[Board].Name}, wave {w.Id})"); pocketT = 0; carves = 0; lastSteer = 0; party = false; partyMates = 0; Dropped = false;
            StoryUi.Event("surf:ride");
        }
        float rideT, pocketT; int carves; float lastSteer; bool party; int partyMates;
        public int LastScore; public bool Dropped, LastParty;         // (tests)
        static readonly float[] ScoreMul = { 0, 1f, 0.8f, 1.1f, 1.3f };

        void Ride(float dt)
        {
            rideT += dt;
            int k = SurfWaves.Find(WaveId); var sp = SurfWaves.Live;
            // co-op: crewmates on this wave: a party wave; one within 2.5 m: a drop-in, both off
            int here = 0;
            foreach (var m in Crew())
            {
                party = true; here++;
                if ((m.transform.position - Pos).magnitude < 2.5f) { Dropped = true; partyMates = Mathf.Max(partyMates, here); Wipe("a drop-in: " + m.NetName); return; }
            }
            partyMates = Mathf.Max(partyMates, here);                     // (a party wave: anyone on it with me at any time in the ride)
            if (k < 0 || sp == null) { RideOver("the wave's gone"); return; }
            var w = SurfWaves.W[k];
            float steer = Ctl.Steer;
            Theta = Mathf.Clamp(Theta + steer * Turn[Board] * dt, -150, 150);
            SurfWaves.Local(k, U, X, out float bs, out float curl, out float wface);
            // the face's slope where the board is (from the bump either side along the way in)
            var p0 = WavePoint(sp, w, U, X - 0.3f); var p1 = WavePoint(sp, w, U, X + 0.3f);
            float slope = (SurfWaves.Bump(p0.x, p0.z) - SurfWaves.Bump(p1.x, p1.z)) / 0.6f;   // + : downhill toward the shore
            float ct = Mathf.Cos(Theta * Mathf.Deg2Rad), st = Mathf.Sin(Theta * Mathf.Deg2Rad);
            // the pocket: the green face within a few metres of a curl
            float dc = Mathf.Min(Mathf.Abs(U - (w.U0 + w.BR)), Mathf.Abs(U - (w.U0 - w.BL)));
            InPocket = w.Broke && bs < 0.3f && dc < PocketW[Board] && X > -0.5f && X < wface * 1.5f + 1.2f;   // (by the curl, on the face or just in front of its foot)
            if (InPocket) pocketT += dt;
            if (Mathf.Abs(steer) > 0.5f && Mathf.Sign(steer) != Mathf.Sign(lastSteer)) { carves++; lastSteer = steer; }
            float a = 9.8f * Mathf.Clamp(slope, -0.8f, 1.2f) * 0.55f * Mathf.Max(ct, -0.5f) - 0.25f * Speed + (InPocket ? 2.6f : 0) + (Mathf.Abs(st) > 0.5f && X < wface ? 0.6f : 0);
            float vmax = Board == 2 && bs > 0.5f ? 5f : VMax[Board];
            Speed = Mathf.Clamp(Speed + a * dt, 1.2f, vmax);
            // the wave carries the board fully at its crest, less and less down the face, not at all on the flat in front:
            // along the face (~60 degrees) the board settles mid-face (trimming); straight down it outruns the wave
            float carry = X < -1.2f ? 0 : X <= 0 ? 1 : 1 - Smooth01(X / (wface * 1.3f));
            X += (SurfWaves.Speed * carry + Speed * ct - SurfWaves.Speed) * dt;
            U += Speed * st * dt;
            Lean = Mathf.Lerp(Lean, -steer * 24f * Mathf.Clamp01(Speed / vmax + 0.3f), dt * 6);
            Pos = WavePoint(sp, w, U, X);
            if (Random.value < dt * 10 * Mathf.Clamp01(Speed / 6)) Fx.Splash(Pos - new Vector3(st, 0, ct) * 0.6f, 0.2f, 2);
            // the ends
            if (Mathf.Abs(Theta) > 125) { Wipe("too far round: over the falls"); return; }
            if (bs > 0.6f && X > -1.5f && X < 1.2f && Board != 2) { Wipe("caught by the white water"); return; }
            if (X < -2.5f) { RideOver("off the back"); return; }
            if (w.S + X > sp.Shore - 6) { RideOver("on the sand"); return; }
            if (Mathf.Abs(U) > sp.Half) { RideOver("off the end of the wave"); return; }
            if (X > wface * 4 && Speed * ct < SurfWaves.Speed * 0.3f) { RideOver("it went by"); return; }
            Band(InPocket, sp, w);
            // the camera looks along the line
            if (CameraRig.I != null) CameraRig.I.Yaw = Mathf.LerpAngle(CameraRig.I.Yaw, Yaw, dt * 2.5f);
        }
        static float Smooth01(float u) { u = Mathf.Clamp01(u); return u * u * (3 - 2 * u); }
        void RideOver(string why)
        {
            LastRide = rideT; LastOutcome = "ride"; Band(false);
            int pts = Score(why == "on the sand");
            Alerts.Push(party ? "Party wave!" : "Ride", $"{rideT:F1} s{(pocketT > 0.3f ? $" · {pocketT:F1} s in the pocket" : "")} · {pts} pts{(why == "on the sand" ? " · all the way in" : "")}", Alerts.Kind.Info);
            Log($"ride over ({why}) after {rideT:F1} s"); WaveId = -1; Speed = 0.5f; Lean = 0; Go(St.Paddle);
            StoryUi.Event("surf:rode");
        }
        void Wipe(string why)
        {
            LastRide = rideT; LastOutcome = "wipe"; Band(false); Score(false);
            Snd.Play(Synth.Splash, Pos, 0.9f, 0.9f); Fx.Splash(Pos, 1.1f, 24); Snd.Rumble(0.7f, 0.5f, 0.3f);
            Alerts.Push("Wipeout", why == "too far round: over the falls" ? "Over the falls. Lean less." : "The white water got you. Stay ahead of the curl.", Alerts.Kind.Info);
            Log($"wipeout ({why}) after {rideT:F1} s"); WaveId = -1; Speed = 0; Go(St.Wipe);
        }
        // a ride's points: time, the pocket counted 3x, a carve 2 pts, all the way in +20%, the board (the shortboard 1.3x),
        // a party wave +50% each crewmate on it; my best and the crew's challenges (Kai: 3 s up, 8 s, 3 s in the pocket)
        int Score(bool allIn)
        {
            int mates = partyMates;
            float pts = (10 * (rideT + 2 * pocketT) + 2 * carves) * ScoreMul[Board] * (allIn ? 1.2f : 1) * (1 + 0.5f * mates);
            LastScore = Mathf.RoundToInt(pts); LastParty = mates > 0;
            var k = Firm.I?.MyKit; if (k == null) return LastScore;
            if (rideT >= 1) k.Rides++;
            if (LastScore > k.SurfBest) k.SurfBest = LastScore;
            var c = Firm.C;
            if (rideT >= 3 && !c.Seen("kai:ch1")) StoryUi.Event("kai:ch1");
            if (rideT >= 8 && !c.Seen("kai:ch2")) StoryUi.Event("kai:ch2");
            if (pocketT >= 3 && !c.Seen("kai:ch3")) StoryUi.Event("kai:ch3");
            if (mates > 0) StoryUi.Event("surf:party");
            Log($"score {LastScore} (ride {rideT:F1} s, pocket {pocketT:F1} s, carves {carves}, {SurfGear.Boards[Board].Name}{(allIn ? ", all the way in" : "")}{(mates > 0 ? $", a party wave with {mates}" : "")})");
            return LastScore;
        }
        // crewmates riding the same wave as me now (their pose: State 7, Up = Ride, Hips.x = the wave's id)
        System.Collections.Generic.IEnumerable<Angler> Crew()
        {
            if (!Coop.On || Coop.I == null || WaveId < 0) yield break;
            foreach (var kv in Coop.I.Mates)
            {
                var m = kv.Value; if (m == null || m.Surf == null) continue;
                if (m.Surf.netOn && (St)(m.Surf.netUp & 7) == St.Ride && m.Surf.netWave == WaveId) yield return m;
            }
        }
        // the tumble: under for a second (bubbles), up, the leash brings the board back
        void Wiping(float dt)
        {
            if (Random.value < dt * 8) Fx.Splash(Pos + Random.insideUnitSphere * 0.6f, 0.15f, 2);
            if (StateT > 2.2f) { Go(St.Paddle); Log("back on the board"); }
        }

        // a point on wave w: u along its crest, x from the crest toward the shore, on the sea's surface
        static Vector3 WavePoint(SurfWaves.Spot sp, SurfWaves.Wave w, float u, float x)
        {
            var p = sp.O + sp.Dir * (w.S + x) + new Vector2(sp.Dir.y, -sp.Dir.x) * u;
            return new Vector3(p.x, Sea.Height(p.x, p.y), p.y);
        }

        // the board and me where the physics says
        void Place()
        {
            var sp = SurfWaves.Live;
            if (State == St.Ride && sp != null)
            {
                var din = new Vector3(sp.Dir.x, 0, sp.Dir.y); var lat = new Vector3(sp.Dir.y, 0, -sp.Dir.x);
                var h = din * Mathf.Cos(Theta * Mathf.Deg2Rad) + lat * Mathf.Sin(Theta * Mathf.Deg2Rad);
                Yaw = Mathf.Atan2(h.x, h.z) * Mathf.Rad2Deg;
            }
            if (State != St.Ride && State != St.Pop) Pos.y = Sea.Height(Pos);
            float sink = duckT >= 0 ? 0.65f * Mathf.Clamp01(duckT / 0.25f) : State == St.Wipe ? (StateT < 1 ? 0.9f : 0.3f) : 0;
            var n = Sea.Normal(Pos.x, Pos.z, 0.6f);
            var rot = Quaternion.FromToRotation(Vector3.up, n) * Quaternion.Euler(0, Yaw, 0);
            if (board != null)
            {
                var bp = State == St.Wipe ? Pos + Quaternion.Euler(0, Yaw + 70, 0) * Vector3.forward * Mathf.Lerp(3.5f, 0.3f, Mathf.Clamp01((StateT - 0.6f) / 1.4f)) : Pos;
                board.SetPositionAndRotation(bp + Vector3.up * (0.02f - sink), rot * Quaternion.Euler(0, 0, Lean));
            }
            transform.SetPositionAndRotation(Pos + Vector3.up * (0.08f - sink - (State == St.Wipe ? 0.5f : 0)), Quaternion.Euler(0, Yaw, 0));
        }

        // the body: lying and stroking (paddle, the bodyboard), crouched side-on (riding), tumbling (wipe)
        public void Pose(ref CharAnim.Slot slot, ref float animSp)
        {
            var st = Active ? State : (St)(netUp & 7); int b = Active ? Board : netUp >> 4; float lean = Active ? Lean : netT;
            var a = M1Assets.I; me.Anim.Grip = null;
            bool standing = (st == St.Ride || st == St.Pop && PopNeedle > 0.85f) && b != 2;
            if (standing && a.JumpUp != null)
            {
                slot = CharAnim.Slot.Idle; animSp = 0;
                me.Anim.Over = a.JumpUp; me.Anim.OverTime = 0.2f; me.Anim.OverW = 1;      // the jump's crouch: knees bent, arms out
                me.Model.localRotation = Quaternion.Euler(0, -80, 0) * Quaternion.Euler(0, 0, lean * 0.8f);   // side-on to the board
                me.Model.localPosition = new Vector3(0, 0.1f, 0);
            }
            else if (st == St.Wipe) { slot = CharAnim.Slot.Swim; animSp = 1; me.Anim.Flail = true; me.Anim.Over = null; me.Model.localRotation = Quaternion.Euler(-30, 0, 0); }
            else
            {
                slot = CharAnim.Slot.Swim; animSp = st == St.Paddle && (Active ? Speed > 0.3f : true) ? 1.2f : 0.2f;
                me.Anim.Over = null; me.Anim.OverW = 0;
                me.Model.localPosition = new Vector3(0, me.Anim.WaterLift(true, Angler.SwimDepth) - Angler.SwimDepth + 0.16f, 0);   // (the swim pose lifts from 1.3 m under: on the board it's at the surface)
                me.Model.localRotation = Quaternion.Euler(0, 0, lean * 0.6f);
            }
        }
        // the prompt line's keys while I'm on the board (Hud.Prompts)
        public System.Collections.Generic.List<(string, string)> Keys() => State switch
        {
            St.Paddle => new() { ("W", "Paddle"), ("Ctrl", "Duck under"), ("E", "Off the board") },
            St.Pop => new() { ("Space", "Stand up") },
            St.Ride => new() { ("A/D", InPocket ? "Carve · in the pocket!" : "Carve along the wave") },
            St.Wipe => new() { ("", "Wipeout!") },
            _ => new(),
        };
        // what the prompt line says while I'm on the board
        public string Hint => State switch
        {
            St.Paddle => "W  Paddle   ·   Ctrl  Duck under   ·   E  Off the board",
            St.Pop => "Space  Stand up",
            St.Ride => InPocket ? "In the pocket!   ·   A/D  Carve" : "A/D  Carve along the wave",
            St.Wipe => "Wipeout",
            _ => null,
        };
        public byte NetUp => (byte)((byte)State | Board << 4);
        public float NetT => Lean;
        // a crewmate's surfing, from their pose
        public void Net(bool on, byte up, float t, Vector3 wave)
        {
            if (!me.Remote) return;
            netWave = Mathf.RoundToInt(wave.x);
            if (on != netOn) { netOn = on; ShowBoard(on ? up >> 4 : 0); }
            netUp = up; netT = t;
            if (board != null && on)
            {
                var p = transform.position; var n = Sea.Normal(p.x, p.z, 0.6f);
                board.SetPositionAndRotation(new Vector3(p.x, Sea.Height(p) + 0.02f, p.z), Quaternion.FromToRotation(Vector3.up, n) * Quaternion.Euler(0, transform.eulerAngles.y, t));
            }
        }

        void ShowBoard(int b)
        {
            if (board != null) { Destroy(board.gameObject); board = null; }
            if (b < 1 || b > 4) return;
            var pf = M1Assets.I != null ? M1Assets.I.M5Prefab("Prop_board_" + Model[b]) : null;
            var g = pf != null ? Instantiate(pf) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            foreach (var c in g.GetComponentsInChildren<Collider>()) Destroy(c);
            if (pf != null) Util.Toon(g, Game.ToonMat); else g.transform.localScale = new Vector3(0.55f, 0.08f, 2.2f);
            if (Firm.C != null && Firm.C.Seen("kai:ch2"))                                         // Kai's sunburst (challenge 2): warm on every board
            { var mpb = new MaterialPropertyBlock(); mpb.SetColor("_BaseColor", new Color(1f, 0.82f, 0.55f)); foreach (var r in g.GetComponentsInChildren<Renderer>()) r.SetPropertyBlock(mpb); }
            g.name = "surfboard " + Model[b]; board = g.transform;
        }

        // ---- the pop-up bar and the pocket's glow
        void Pop(bool on)
        {
            if (on && popUi == null && Hud.I != null)
            {
                popUi = UiKit.Card((RectTransform)Hud.I.transform, "surf pop", 14, Ponyo.Paper, true, Ponyo.Line, 4);
                popUi.Place(new Vector2(0.5f, 0), new Vector2(0, 230), new Vector2(560, 46), new Vector2(0.5f, 0));
                var track = UiKit.Img(popUi, null, Ponyo.Hex("CFF2F0"), "track").rectTransform; track.Stretch(8);
                popZone = UiKit.Img(track, null, Ponyo.Leaf, "zone").rectTransform;
                popPtr = UiKit.Img(track, null, Ponyo.Ink, "ptr").rectTransform; popPtr.pivot = new Vector2(0.5f, 0.5f);
            }
            if (popUi != null) { popUi.gameObject.SetActive(on); if (on) PopShow(0.62f); }
        }
        void PopShow(float c0)
        {
            if (popUi == null) return;
            popZone.anchorMin = new Vector2(Mathf.Clamp01(c0 - popGreen / 2), 0); popZone.anchorMax = new Vector2(Mathf.Clamp01(c0 + popGreen / 2), 1); popZone.offsetMin = popZone.offsetMax = Vector2.zero;
            popPtr.anchorMin = popPtr.anchorMax = new Vector2(PopNeedle, 0.5f); popPtr.sizeDelta = new Vector2(6, 40); popPtr.anchoredPosition = Vector2.zero;
        }
        void Band(bool on, SurfWaves.Spot sp = null, SurfWaves.Wave w = default)
        {
            if (!on) { if (band != null) band.enabled = false; return; }
            if (band == null)
            {
                band = new GameObject("pocket band").AddComponent<LineRenderer>(); band.positionCount = 12; band.useWorldSpace = true;
                band.widthMultiplier = 0.9f; band.sharedMaterial = Util.Soft(new Color(1f, 0.85f, 0.35f, 0.45f)); band.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            band.enabled = true;
            bool right = Mathf.Abs(U - (w.U0 + w.BR)) < Mathf.Abs(U - (w.U0 - w.BL));
            float cu = right ? w.U0 + w.BR : w.U0 - w.BL, sgn = right ? 1 : -1;
            for (int i = 0; i < 12; i++)
            {
                float u = cu + sgn * (0.5f + i * PocketW[Board] / 11f), x = 0.5f + 0.25f * w.H;
                band.SetPosition(i, WavePoint(sp, w, u, x) + Vector3.up * 0.12f);
            }
        }
        void OnDestroy() { if (board != null) Destroy(board.gameObject); if (band != null) Destroy(band.gameObject); if (popUi != null) Destroy(popUi.gameObject); }
    }
}

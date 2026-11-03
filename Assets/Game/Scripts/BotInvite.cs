using System.Collections;
using System.IO;
using UnityEngine;

namespace PortBoss.M1
{
    public partial class Bot
    {
        // -pb-invitetest <dir> (M3.1, with -pb-host: tools/invitetest.sh). The invite list without Steam's overlay: I in a
        // LAN crew (the address to share), then as a Steam crew with made-up friends (the overlay off, as on Manan's PC):
        // the list, the left-side note saying why Steam's window didn't open, inviting one, the crew code in the pause menu;
        // the crew code round trip.
        IEnumerator InviteTest(string dir)
        {
            shots = dir; Directory.CreateDirectory(dir);
            var said = new System.Collections.Generic.List<string>(); Alerts.OnPush += a => said.Add(a.Text);
            bool ok = true; void Check(string w, bool c) { Debug.Log($"[PB] invite: {w}: {(c ? "ok" : "FAIL")}"); ok &= c; }
            for (float t = 0; !Coop.On && t < 10; t += Time.deltaTime) yield return null;
            yield return new WaitForSeconds(1.5f);
            // the crew code: every lobby id there and back, typed sloppily too
            bool rt = true; var rnd = new System.Random(7);
            for (int i = 0; i < 2000; i++)
            {
                ulong id = 0x0186000000000000UL | (ulong)(uint)rnd.Next() << 1 | (uint)rnd.Next(2);
                if (Coop.IdOf(Coop.CodeOf(id)) != id || Coop.IdOf(Coop.CodeOf(id).ToLowerInvariant().Replace("-", " ")) != id) { rt = false; Debug.Log($"[PB] invite: code round trip broke on {id}"); break; }
            }
            ulong odd = 0x0123456789ABCDEFUL; rt &= Coop.IdOf(Coop.CodeOf(odd)) == odd;
            Check($"crew codes go there and back (2000 lobby ids, typed lower case with spaces; a non-lobby id written whole: {Coop.CodeOf(odd)})", rt);
            Check("misread letters still join: O for 0, I and L for 1", Coop.IdOf("0I1-OL00") == Coop.IdOf("011-0100"));

            // the button under the crew list (top right): always there in a crew with room
            Check("an Invite a friend button (I) shows under the crew list", NameTags.I != null && NameTags.I.InviteShown);
            yield return Shot("invite-button");
            // a crew on this network: I opens the list with the address to share
            Coop.Invite(); yield return new WaitForSeconds(0.6f);
            Check($"LAN crew: I opens the invite list ({Title.I?.RowText(0)})", Title.Up && !Title.Shown && Title.I.RowText(0).Contains("Friends join"));
            Check($"the world runs on under it (time {Time.timeScale})", Time.timeScale == 1);
            yield return Shot("invite-lan");
            Title.CloseInvite(); yield return null;
            Check("Esc / Back closes it", !Title.Up);

            // a Steam crew, overlay off (as on Manan's PC: started outside Steam)
            Coop.TestLobby = 0x018600000ABC1234UL;
            Coop.TestFriends = new() { ("Tushar", "Playing now", 76561190000000001UL), ("Asha", "Online", 76561190000000002UL), ("Dev", "Online", 76561190000000003UL) };
            said.Clear();
            Coop.Invite(); yield return new WaitForSeconds(0.8f);
            string rows = ""; for (int i = 0; i < Title.I.Rows; i++) rows += "\n    " + Title.I.RowText(i);
            Debug.Log("[PB] invite: the list:" + rows);
            Check("Steam crew: copy the code, Steam's window (off: no overlay), 3 friends, back", Title.I.Rows == 6 && Title.I.RowText(1).Contains("(off)") && Title.I.RowText(2).StartsWith("Invite Tushar"));
            Check($"a note on the left says why Steam's window didn't open (crew code {Coop.Code})", said.Exists(t => t.Contains("overlay")));
            yield return Shot("invite-steam-list");
            Title.I.Choose(2); yield return new WaitForSeconds(0.8f);
            Check("picking Tushar sends him the invite (Lobby.InviteFriend: no overlay needed)", said.Exists(t => t.Contains("Invite sent to Tushar")));
            yield return Shot("invite-sent");
            Title.CloseInvite(); yield return null;
            // the pause menu shows the crew code
            var g = FindAnyObjectByType<Game>(); g.ShowPause(true); yield return new WaitForSeconds(0.3f);
            Check($"the pause menu shows I and the crew code: {g.PauseLine.Replace("\n", " / ")}", g.PauseLine.Contains("I  invite friends") && g.PauseLine.Contains(Coop.Code));
            yield return Shot("pause-crew-code");
            g.ShowPause(false);
            Coop.TestLobby = 0; Coop.TestFriends = null;
            Debug.Log($"[PB] RESULT invite: {(ok ? "PASS" : "FAIL")}");
            Application.Quit();
        }
    }
}

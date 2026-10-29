using UnityEngine;

namespace HollowCurrent
{
    // Append only: values are serialized in prepared scenes. Van = the map's escape
    // (Bellwether case terminal, castle drawbridge winch); Anchor = music box / chapel bell.
    // fb-onboard: Nailed = a Long Night hide that is nailed shut (CastleWorld.NailHides); E says so instead of doing nothing.
    public enum InteractKind { Kit, Note, Door, Hide, Breaker, Anchor, Van, LightSwitch, Relic, Altar, Sarcophagus, Nailed }
    public sealed class Interactable : MonoBehaviour
    {
        public InteractKind Kind;
        public string Label, Body;
        public Vector3 HidePoint, ExitPoint;
        public Door Door;
        // The line the HUD shows after the key: a door reads "Open Hall great doors" / "Close ..." (fb-doors); everything else is its Label.
        public string Prompt=>Kind==InteractKind.Door&&Door?(Door.Open?"Close ":"Open ")+Label:Label;
        public string PromptFor(Game game)
        {
            if(Kind==InteractKind.Altar&&game.Castle)return game.Castle.Forge.HasSword?"Forge altar · sword ready":game.Castle.Forge.RelicCount==3?"Forge sword · hold 3 seconds":"Forge altar · "+game.Castle.Forge.RelicCount+"/3 relics";
            if(Kind==InteractKind.Anchor)return game.Case.Phase==Phase.Containment?(game.Case.BreakerOn?"Hold 3 seconds · "+Label:"Restore power first · "+Label):game.Map==MapId.Bellwether?"Listen to the music box":Prompt;
            return Prompt;
        }
        public Light Circuit;
        // Kind Relic: 0 silver blade, 1 hallowed hilt, 2 moonstone (Forge.Relic order).
        public int RelicIndex=-1;
        // Kind Relic: the visible relic mesh (hidden together with the interactable when taken).
        public GameObject Art;
        public void Use(Game game)
        {
            switch(Kind)
            {
                // fb-onboard: the castle equips at spawn, so the rack only answers (never a silent E). Bellwether: unchanged.
                case InteractKind.Kit: if(!game.Case.Equip()&&game.Map==MapId.Castle)game.Notice("The rack is empty. You already carry the instruments."); break;
                case InteractKind.Note: game.Case.Read(Label);game.ShowNote(Label,Body);break;
                case InteractKind.Door: Door.Toggle("E");break;
                case InteractKind.Hide: game.Player.Hide(this);break;
                case InteractKind.LightSwitch: game.ToggleCircuit(Circuit,transform.position);break;
                case InteractKind.Breaker: game.Case.RestorePower();game.Sound.Cue("switch",transform.position);break;
                case InteractKind.Van:
                    if(game.Castle&&(game.Castle.ResolveDeathBeforeWin()||game.Creature&&game.Creature.GrabCamActive))break;if(!game.Case.FinishAtVan())game.ShowNote(game.Book.FinishNoteTitle,game.HeldGoal()+game.Book.FinishNoteSuffix);break; // castle: held goal (critic r10b N1)
                case InteractKind.Relic: if(game.Castle)game.Castle.TakeRelic(this);break;
                case InteractKind.Altar: if(game.Castle)game.Castle.AltarHint();break;
                // Castle crypt (FEAR_PLAN item 18, the cursed reading): CastleRun.Sarcophagus() once hc/count11 lands.
                case InteractKind.Sarcophagus: if(game.Castle)game.Castle.Sarcophagus();break;
                case InteractKind.Nailed: game.Notice("Nailed shut. Find another hiding place.");game.Sound.Cue("door_rattle",transform.position,.6f);break;
                case InteractKind.Anchor:
                    if(game.Castle){game.Castle.RingBell();break;}
                    if(game.Case.Phase!=Phase.Containment)
                    {
                        if(game.Case.Record(Evidence.Voice)){game.Sound.Cue("voice",game.World.Voice);game.Notice("Recorded: voice at the music box · "+game.Case.Evidence.Count+"/3",4);}
                        else game.Notice(game.Case.Diagnosed?"Restore the basement breaker, then hold the lid closed.":"Use the reader in the dining room and ultraviolet at the upstairs bedroom window.",5);
                    }
                    else if(!game.Case.BreakerOn)game.Notice("Restore power at the basement fuse cabinet first.",4);
                    break;
            }
        }
    }
}

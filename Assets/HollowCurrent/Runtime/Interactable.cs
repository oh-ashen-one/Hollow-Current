using UnityEngine;

namespace HollowCurrent
{
    // Append only: values are serialized in prepared scenes. Van = the map's escape
    // (Bellwether case terminal, castle drawbridge winch); Anchor = music box / chapel bell.
    public enum InteractKind { Kit, Note, Door, Hide, Breaker, Anchor, Van, LightSwitch, Relic, Altar }
    public sealed class Interactable : MonoBehaviour
    {
        public InteractKind Kind;
        public string Label, Body;
        public Vector3 HidePoint, ExitPoint;
        public Door Door;
        public Light Circuit;
        // Kind Relic: 0 silver blade, 1 hallowed hilt, 2 moonstone (Forge.Relic order).
        public int RelicIndex=-1;
        // Kind Relic: the visible relic mesh (hidden together with the interactable when taken).
        public GameObject Art;
        public void Use(Game game)
        {
            switch(Kind)
            {
                case InteractKind.Kit: game.Case.Equip(); break;
                case InteractKind.Note: game.Case.Read(Label);game.ShowNote(Label,Body);break;
                case InteractKind.Door: Door.Toggle();break;
                case InteractKind.Hide: game.Player.Hide(this);break;
                case InteractKind.LightSwitch: game.ToggleCircuit(Circuit,transform.position);break;
                case InteractKind.Breaker: game.Case.RestorePower();game.Sound.Cue("switch",transform.position);break;
                case InteractKind.Van:
                    if(!game.Case.FinishAtVan())game.ShowNote(game.Book.FinishNoteTitle,game.Case.Goal+game.Book.FinishNoteSuffix);break;
                case InteractKind.Relic: if(game.Castle)game.Castle.TakeRelic(this);break;
                case InteractKind.Altar: if(game.Castle)game.Castle.AltarHint();break;
                case InteractKind.Anchor:
                    if(game.Castle){game.Castle.RingBell();break;}
                    if(game.Case.Phase!=Phase.Containment)game.Say(game.Book.AnchorNotReady);break;
            }
        }
    }
}

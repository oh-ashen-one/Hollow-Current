using UnityEngine;

namespace HollowCurrent
{
    public enum InteractKind { Kit, Note, Door, Hide, Breaker, Anchor, Van, LightSwitch }
    public sealed class Interactable : MonoBehaviour
    {
        public InteractKind Kind;
        public string Label, Body;
        public Vector3 HidePoint, ExitPoint;
        public Door Door;
        public Light Circuit;
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
                    if(!game.Case.FinishAtVan())game.ShowNote("CASE 014",game.Case.Goal+"\n\nThe van is safe. Equipment never runs out of charge. Take the time you need.");break;
                case InteractKind.Anchor:
                    if(game.Case.Phase!=Phase.Containment)game.Say("A tiny tune circles inside the box. Reconstruct what holds the house in your journal before attempting a seal.");break;
            }
        }
    }
}

﻿using SabberStoneCore.Actions;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;

namespace SabberStoneCore.Tasks.SimpleTasks
{
    public class DrawOpTask : SimpleTask
    {
        public DrawOpTask(Card card = null, bool toStack = false)
        {
            Card = card;
            ToStack = toStack;
        }

        public Card Card { get; set; }

        public bool ToStack { get; set; }

        public override ETaskState Process()
        {
            var drawedCard = Card != null ? Generic.DrawCardBlock.Invoke(Controller.Opponent, Card) : Generic.Draw(Controller.Opponent);
            if (ToStack && drawedCard != null)
            {
                Playables.Add(drawedCard);
            }
            return ETaskState.COMPLETE;
        }

        public override ISimpleTask Clone()
        {
            var clone = new DrawOpTask(Card, ToStack);
            clone.Copy(this);
            return clone;
        }
    }
}
using System;

namespace FivePebblesPong
{
    public class Enums
    {
        //https://rainworldmodding.miraheze.org/wiki/Downpour_Reference/ExtEnum

        //type for spawning controller
        public static AbstractPhysicalObject.AbstractObjectType GameControllerPebbles;
        public static AbstractPhysicalObject.AbstractObjectType GameControllerMoon;

        //five pebbles action
        public static SSOracleBehavior.Action Gaming_Gaming;

        //puppet movement (controlled by FPGame subclass)
        public static SSOracleBehavior.MovementBehavior SSPlayGame;
        public static SLOracleBehavior.MovementBehavior SLPlayGame;

        //moon reaction on controller
        public static SLOracleBehaviorHasMark.MiscItemType GameControllerPebblesReaction;
        public static SLOracleBehaviorHasMark.MiscItemType GameControllerMoonReaction;


        public static void RegisterValues()
        {
            GameControllerPebbles = new AbstractPhysicalObject.AbstractObjectType("GameControllerPebbles", true);
            GameControllerMoon = new AbstractPhysicalObject.AbstractObjectType("GameControllerMoon", true);
            Gaming_Gaming = new SSOracleBehavior.Action("Gaming_Gaming", true);
            SSPlayGame = new SSOracleBehavior.MovementBehavior("SSPlayGame", true);
            SLPlayGame = new SLOracleBehavior.MovementBehavior("SLPlayGame", true);
            GameControllerPebblesReaction = new SLOracleBehaviorHasMark.MiscItemType("GameControllerPebblesReaction", true);
            GameControllerMoonReaction = new SLOracleBehaviorHasMark.MiscItemType("GameControllerMoonReaction", true);
        }


        public static void UnregisterValues()
        {
            if (GameControllerPebbles != null) { GameControllerPebbles.Unregister(); GameControllerPebbles = null; }
            if (GameControllerMoon != null) { GameControllerMoon.Unregister(); GameControllerMoon = null; }
            if (Gaming_Gaming != null) { Gaming_Gaming.Unregister(); Gaming_Gaming = null; }
            if (SSPlayGame != null) { SSPlayGame.Unregister(); SSPlayGame = null; }
            if (SLPlayGame != null) { SLPlayGame.Unregister(); SLPlayGame = null; }
            if (GameControllerPebblesReaction != null) { GameControllerPebblesReaction.Unregister(); GameControllerPebblesReaction = null; }
            if (GameControllerMoonReaction != null) { GameControllerMoonReaction.Unregister(); GameControllerMoonReaction = null; }
        }
    }
}

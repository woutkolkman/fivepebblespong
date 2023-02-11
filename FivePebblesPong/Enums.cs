using System;

namespace FivePebblesPong
{
    public class Enums
    {
        //https://rainworldmodding.miraheze.org/wiki/Downpour_Reference/ExtEnum

        //type for spawning controller
        public static AbstractPhysicalObject.AbstractObjectType GameController;

        //five pebbles action
        public static SSOracleBehavior.Action Gaming_Gaming;

        //five pebbles movement (controlled by FPGame subclass)
        public static SSOracleBehavior.MovementBehavior PlayGame;

        //moon reaction on controller
        public static SLOracleBehaviorHasMark.MiscItemType GameControllerReaction;


        public static void RegisterValues()
        {
            GameController = new AbstractPhysicalObject.AbstractObjectType("GameController", true);
            Gaming_Gaming = new SSOracleBehavior.Action("Gaming_Gaming", true);
            PlayGame = new SSOracleBehavior.MovementBehavior("PlayGame", true);
            GameControllerReaction = new SLOracleBehaviorHasMark.MiscItemType("GameControllerReaction", true);
        }


        public static void UnregisterValues()
        {
            if (GameController != null) { GameController.Unregister(); GameController = null; }
            if (Gaming_Gaming != null) { Gaming_Gaming.Unregister(); Gaming_Gaming = null; }
            if (PlayGame != null) { PlayGame.Unregister(); PlayGame = null; }
            if (GameControllerReaction != null) { GameControllerReaction.Unregister(); GameControllerReaction = null; }
        }
    }
}

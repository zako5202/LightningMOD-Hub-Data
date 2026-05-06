using Turbo.Plugins.Default;
using System;

namespace Turbo.Plugins.RNN
{
    public class WallRectangleOutlinePlugin : BasePlugin, IInGameWorldPainter
    {
        private IBrush Line;

        private double toRad = Math.PI / 180d;

        public double WallWidth { get; set; } = 1.5d;
        public double WallLength { get; set; } = 18d;

        public WallRectangleOutlinePlugin()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            // Ice-blue color (ARGB: 255, 100, 180, 255)
            Line = Hud.Render.CreateBrush(255, 100, 180, 255, 3);

        }

        public void PaintWorld(WorldLayer layer)
        {
            if (!Hud.Game.IsInGame || Hud.Game.Me.IsInTown)
                return;

            foreach (var actor in Hud.Game.Actors)
            {
                if (actor.SnoActor.Sno != ActorSnoEnum._monsteraffix_waller_model)
                    continue;

                float rot = Direction(actor);
                DrawRectangle(Line, actor.FloorCoordinate, rot);
            }
        }

        private float Direction(IActor actor)
        {
            var dx = actor.FloorCoordinate.X - actor.CollisionCoordinate.X;
            var dy = actor.FloorCoordinate.Y - actor.CollisionCoordinate.Y;

            if (dx == 0 && dy == 0)
                return -45f;

            return (float)(Math.Atan2(dy, dx) / toRad) - 45f;
        }

        private void DrawRectangle(IBrush brush, IWorldCoordinate center, float rotation)
        {
            var L = WallLength;
            var W = WallWidth;

            var radius = ((float)Math.Sqrt(L * L + W * W)) * 0.5f;

            var angle1 = rotation * toRad + Math.Atan2(W, L);
            var angle2 = rotation * toRad - Math.Atan2(W, L);

            var x1 = radius * (float)Math.Cos(angle1);
            var y1 = radius * (float)Math.Sin(angle1);

            var x2 = radius * (float)Math.Cos(angle2);
            var y2 = radius * (float)Math.Sin(angle2);

            var w1 = center.Offset(x1, y1, 0);
            var w3 = center.Offset(-x1, -y1, 0);

            var w2 = center.Offset(x2, y2, 0);
            var w4 = center.Offset(-x2, -y2, 0);

            // Draw crisp rectangle outline
            brush.DrawLineWorld(w1, w2);
            brush.DrawLineWorld(w2, w3);
            brush.DrawLineWorld(w3, w4);
            brush.DrawLineWorld(w4, w1);
        }
    }
}

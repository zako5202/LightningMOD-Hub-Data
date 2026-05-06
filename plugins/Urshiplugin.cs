using System.Linq;
using Turbo.Plugins.Default;

namespace Turbo.Plugins.RNN
{
    public class UrshiPlugin : BasePlugin, IInGameWorldPainter
    {
        // Toggle label and ground circle independently
        public bool ShowLabel { get; set; } = true;
        public bool ShowGroundCircle { get; set; } = true;

        // Adjustable world‑space offset for label text
        public float LabelOffsetZ { get; set; } = 3f;

        public GroundLabelDecorator UrshiDecorator { get; set; }
        public GroundCircleDecorator UrshiCircle { get; set; }

        // Confirmed SNO for Urshi (gem upgrade NPC after GR boss kill)
        private const uint UrshiSno = 398682;

        public UrshiPlugin()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            UrshiDecorator = new GroundLabelDecorator(Hud)
            {
                TextFont = Hud.Render.CreateFont("tahoma", 10f, 255, 255, 220, 50, true, false, true),
                BackgroundBrush = Hud.Render.CreateBrush(180, 0, 0, 0, 0),
                BorderBrush = Hud.Render.CreateBrush(220, 255, 220, 50, 2),
                ForceOnScreen = false,
                CenterBaseLine = true,

                // OffsetY is not used for world objects anymore
                OffsetY = 0f,
            };

            UrshiCircle = new GroundCircleDecorator(Hud)
            {
                Brush = Hud.Render.CreateBrush(200, 255, 220, 50, 8f),
                Radius = 3f,
            };
        }

        public void PaintWorld(WorldLayer layer)
        {
            if (layer != WorldLayer.Ground) return;
            if (!Hud.Game.IsInGame) return;
            if (Hud.Game.Me.IsInTown) return;
            if (Hud.Game.Me.InGreaterRiftRank <= 0) return;
            if (!ShowLabel && !ShowGroundCircle) return;

            var urshi = Hud.Game.Actors
                .FirstOrDefault(a => (uint)a.SnoActor.Sno == UrshiSno);

            if (urshi == null) return;
            if (!urshi.IsOnScreen) return;

            // Label with adjustable world‑space offset
            if (ShowLabel)
            {
                var labelCoord = urshi.FloorCoordinate.Offset(0, 0, LabelOffsetZ);
                UrshiDecorator.Paint(null, labelCoord, "Urshi");
            }

            // Circle (already world‑space)
            if (ShowGroundCircle)
                UrshiCircle.Paint(urshi, urshi.FloorCoordinate, null);
        }
    }
}

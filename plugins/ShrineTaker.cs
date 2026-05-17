using System;
using System.Linq;
using Turbo.Plugins.Default;

namespace Turbo.Plugins.zako
{
    public class DAV_ShrineTaker : BasePlugin, IInGameWorldPainter
    {
        public string Message_ByMe { get; set; }
        public Func<string, string> Message_ByOther { get; set; }
        public WorldDecoratorCollection TakerDecorator { get; set; }

        public DAV_ShrineTaker()
        {
            Enabled = true;
        }

        public override void Load(IController hud)
        {
            base.Load(hud);

            Message_ByMe = "Token by ME";
            Message_ByOther = (name) => "Token by " + name;

            // ⭐ Same label look as UnpullableAndBlockedElite
            TakerDecorator = new WorldDecoratorCollection(
                new GroundLabelDecorator(Hud)
                {
                    TextFont = Hud.Render.CreateFont("tahoma", 6f, 255, 255, 200, 60, true, false, false),
                    BackgroundBrush = Hud.Render.CreateBrush(160, 0, 0, 0, 0),
                    BorderBrush = Hud.Render.CreateBrush(200, 255, 200, 0, 2),
                    CenterBaseLine = true,
                    OffsetY = -30f,
                    // ⭐ IMPORTANT: remove ForceOnScreen so label stays at shrine
                    ForceOnScreen = false
                }
            );
        }

        public void PaintWorld(WorldLayer layer)
        {
            if (Hud.Game.IsInTown)
                return;

            var shrines = Hud.Game.Shrines
                .Where(x => (x.IsShrine || x.IsPylon) &&
                            (x.IsDisabled || x.IsOperated || x.IsClickable));

            foreach (var actor in shrines)
            {
                var tookID = actor.GetAttributeValueAsUInt(Hud.Sno.Attributes.Gizmo_Operator_ACDID, 2147483647);
                var taker = Hud.Game.Players.FirstOrDefault(y => y.SummonerId == tookID);

                if (taker != null)
                {
                    string msg = taker.IsMe ? Message_ByMe : Message_ByOther(taker.BattleTagAbovePortrait);
                    TakerDecorator.Paint(layer, actor, actor.FloorCoordinate, msg);
                }
            }
        }
    }
}

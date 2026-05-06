using System;
using System.Linq;
using Turbo.Plugins.Default;
 
namespace Turbo.Plugins.DAV
{
    public class DAV_ShrineTaker : BasePlugin, IInGameWorldPainter {


        public const string Version = "1.0.0";
        
        public string Message_ByMe { get; set; }
        public Func<string, string> Message_ByOther { get; set; }
        public WorldDecoratorCollection TakerDecorator { get; set; }
 
        public DAV_ShrineTaker() {
            Enabled = true;
        }
 
        public override void Load(IController hud) {
            base.Load(hud);
 
            Message_ByMe = "Token by ME";
            Message_ByOther = (name) => "Token by " + name;
 
            TakerDecorator = new WorldDecoratorCollection(
                new GroundLabelDecorator(Hud) {
                    ForceOnScreen= false,
                    OffsetY = -120,
                    BackgroundBrush = Hud.Render.CreateBrush(255, 255, 255, 64, 0),
                    TextFont = Hud.Render.CreateFont("Arial", 8.5f, 255, 0, 0, 0, false, false, false),
                }
            );
        }
 
        public void PaintWorld(WorldLayer layer) {
            if (Hud.Game.IsInTown) return;
 
            var shrines = Hud.Game.Shrines.Where(x => (x.IsShrine || x.IsPylon) && (x.IsDisabled || x.IsOperated || x.IsClickable));
            foreach (var actor in shrines) {
                var tookID = actor.GetAttributeValueAsUInt(Hud.Sno.Attributes.Gizmo_Operator_ACDID, 2147483647);
                var taker = Hud.Game.Players.FirstOrDefault(y => y.SummonerId == tookID);
                if (taker != null)
                    TakerDecorator.Paint(layer, actor, actor.FloorCoordinate, taker.IsMe ? Message_ByMe : Message_ByOther(taker.BattleTagAbovePortrait));
            }
        }
    }
}
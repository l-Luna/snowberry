using Celeste;

namespace Snowberry.Editor.Stylegrounds;

[Plugin("tentacles")]
public class Plugin_Tentacles : Styleground{

    [Option("side")] public Tentacles.Side Side = Tentacles.Side.Right;
    [Option("offset")] public float Offset = 0;
}
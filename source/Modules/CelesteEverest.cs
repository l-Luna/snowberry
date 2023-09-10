using Monocle;

namespace Snowberry; 

public class CelesteEverest : SnowberryModule {
    public static SnowberryModule INSTANCE { get; private set; }

    public CelesteEverest()
        : base(name: "Celeste + Everest", Calc.HexToColor("e6342e")) {
        INSTANCE = this;
    }
}
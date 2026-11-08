namespace PixelBuddy;

enum BuddyState { Idle, Run, Katana, Train, Sleep, Alert, Ask }

static class BuddyStateExt
{
    /// Nombre del estado en los archivos de sprites (`idle`, `run`…).
    public static string Key(this BuddyState s) => s.ToString().ToLowerInvariant();
}

/// Un personaje en pantalla (uno por sesión de Claude Code, más el principal).
sealed class Buddy
{
    public const double KatanaDuration = 30;
    public const double SleepAfter = 10 * 60;
    public const double TrainDuration = 60;
    /// El personaje principal se duerme a los 2 min sin actividad de Claude.
    public const double GuardianSleepAfter = 2 * 60;
    public const double ForgetAfter = 2 * 60 * 60;

    public string Id { get; }
    public int ColorIndex { get; }
    public bool IsGuardian { get; }

    /// Estado lógico (lo que dicen los eventos).
    public BuddyState State;
    /// Hora (epoch) en que empezó el estado lógico.
    public double StateSince;
    /// Hora (epoch) del último evento recibido.
    public double LastEvent;

    /// Animación que se ve (puede ser `Run` mientras vuelve a su sitio).
    public BuddyState Anim = BuddyState.Idle;
    public double AnimStart;

    public double X;          // centro del personaje, en puntos desde la izquierda de la ventana
    public double Dir = 1;    // 1 = derecha, -1 = izquierda
    public double HomeX;      // sitio donde se queda quieto
    public double Speed { get; } = 130 + Random.Shared.NextDouble() * 40;

    public string RenderKey = "";

    public Buddy(string id, int colorIndex, bool isGuardian, double now, double mono)
    {
        Id = id; ColorIndex = colorIndex; IsGuardian = isGuardian;
        StateSince = now; LastEvent = now; AnimStart = mono;
    }

    public void SetState(BuddyState s, double since)
    {
        if (s != State || s == BuddyState.Katana) StateSince = since;
        State = s;
    }

    /// Transiciones por tiempo: katana (30 s) → train (1 min, Claude espera tu
    /// mensaje) → sleep. Cualquier otro estado (salvo alert/ask) → sleep a los 10 min.
    public void ApplyTimers(double now)
    {
        if (State == BuddyState.Katana && now - StateSince >= KatanaDuration)
            SetState(BuddyState.Train, StateSince + KatanaDuration);
        if (State == BuddyState.Train && now - StateSince >= TrainDuration)
            SetState(BuddyState.Sleep, StateSince + TrainDuration);
        // Un error o una pregunta no se duermen: siguen avisando hasta que respondas.
        if (State is not (BuddyState.Sleep or BuddyState.Alert or BuddyState.Ask)
            && now - LastEvent >= SleepAfter)
            SetState(BuddyState.Sleep, LastEvent + SleepAfter);
    }
}

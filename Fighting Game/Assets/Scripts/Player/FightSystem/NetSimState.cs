using FightSim;
using Fusion;

public struct NetFighter : INetworkStruct
{
    public int posX;
    public int posY;
    public int velX;
    public int velY;
    public NetworkBool facingRight;

    public FighterStateId stateId;
    public int stateFrame;
    public int attackIndex;
    public NetworkBool attackHasHit;

    public int hitStopFrames;
    public int hitStunFrames;
    public NetworkBool guardCrouch;
    public int health;
    public int comboCount;
    public NetworkBool knockdown;

    public ushort prevButtons;
    public int bufLight;
    public int bufMedium;
    public int bufHeavy;
    public int bufThrow;
    public ulong dirHistory;
}

public struct NetRound : INetworkStruct
{
    // MatchSnapshot
    public int frame;
    public NetFighter p1;
    public NetFighter p2;

    // RoundSnapshot
    public RoundPhase phase;
    public int phaseFrame;
    public int timerFrames;
    public int roundNumber;
    public int wins0;
    public int wins1;
    public RoundEndReason endReason;
    public int roundWinner;
    public int matchWinner;
}

// 1 ティック分の入力
public struct NetInput : INetworkInput
{
    public ushort buttons;
}

public static class NetSimConvert
{
    public static NetFighter ToNet(FighterSimState s)
    {
        NetFighter n = new NetFighter();
        n.posX = s.posX;
        n.posY = s.posY;
        n.velX = s.velX;
        n.velY = s.velY;
        n.facingRight = s.facingRight;
        n.stateId = s.stateId;
        n.stateFrame = s.stateFrame;
        n.attackIndex = s.attackIndex;
        n.attackHasHit = s.attackHasHit;
        n.hitStopFrames = s.hitStopFrames;
        n.hitStunFrames = s.hitStunFrames;
        n.guardCrouch = s.guardCrouch;
        n.health = s.health;
        n.comboCount = s.comboCount;
        n.knockdown = s.knockdown;
        n.prevButtons = (ushort)s.prevButtons;
        n.bufLight = s.bufLight;
        n.bufMedium = s.bufMedium;
        n.bufHeavy = s.bufHeavy;
        n.bufThrow = s.bufThrow;
        n.dirHistory = s.dirHistory;
        return n;
    }

    public static FighterSimState FromNet(NetFighter n)
    {
        FighterSimState s = new FighterSimState();
        s.posX = n.posX;
        s.posY = n.posY;
        s.velX = n.velX;
        s.velY = n.velY;
        s.facingRight = n.facingRight;
        s.stateId = n.stateId;
        s.stateFrame = n.stateFrame;
        s.attackIndex = n.attackIndex;
        s.attackHasHit = n.attackHasHit;
        s.hitStopFrames = n.hitStopFrames;
        s.hitStunFrames = n.hitStunFrames;
        s.guardCrouch = n.guardCrouch;
        s.health = n.health;
        s.comboCount = n.comboCount;
        s.knockdown = n.knockdown;
        s.prevButtons = (InputButtons)n.prevButtons;
        s.bufLight = n.bufLight;
        s.bufMedium = n.bufMedium;
        s.bufHeavy = n.bufHeavy;
        s.bufThrow = n.bufThrow;
        s.dirHistory = n.dirHistory;
        return s;
    }

    public static NetRound ToNet(RoundSnapshot r)
    {
        NetRound n = new NetRound();
        n.frame = r.match.frame;
        n.p1 = ToNet(r.match.p1);
        n.p2 = ToNet(r.match.p2);
        n.phase = r.phase;
        n.phaseFrame = r.phaseFrame;
        n.timerFrames = r.timerFrames;
        n.roundNumber = r.roundNumber;
        n.wins0 = r.wins0;
        n.wins1 = r.wins1;
        n.endReason = r.endReason;
        n.roundWinner = r.roundWinner;
        n.matchWinner = r.matchWinner;
        return n;
    }

    public static RoundSnapshot FromNet(NetRound n)
    {
        MatchSnapshot m = new MatchSnapshot();
        m.frame = n.frame;
        m.p1 = FromNet(n.p1);
        m.p2 = FromNet(n.p2);

        RoundSnapshot r = new RoundSnapshot();
        r.match = m;
        r.phase = n.phase;
        r.phaseFrame = n.phaseFrame;
        r.timerFrames = n.timerFrames;
        r.roundNumber = n.roundNumber;
        r.wins0 = n.wins0;
        r.wins1 = n.wins1;
        r.endReason = n.endReason;
        r.roundWinner = n.roundWinner;
        r.matchWinner = n.matchWinner;
        return r;
    }
}
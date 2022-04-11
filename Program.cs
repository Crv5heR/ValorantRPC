class Program
{
    public static Task Main() => new Program().MainAsync();
    public Auth? Auth { get; set; }
    public DiscordRpcClient? Client { get; set; }
    public UserPresence.Presence? Presence { get; set; }
    //Testing
    // private RichPresence? CurrentPre { get; set; }
    // Vars
    public string? MapName { get; set; }
    public string? GameMode { get; set; }
    private static bool IsNotValorantRunning => Process.GetProcessesByName("VALORANT-Win64-Shipping").Length is 0;
    private void PrepareAuthInst() => Auth = Websocket.GetAuthLocal(false);
    public async Task MainAsync()
    {
        Console.Title = "VALORANT Discord RPC - by CruSheR#2015";
        PrintLogo();
        Console.WriteLine("Checking If Valorant is Running...", Color.DarkRed);
        await Task.Delay(1500);
        if (IsNotValorantRunning)
        {
            Console.WriteLine("VALORANT is not running, please run it before launching the application!", Color.DarkRed);
            Console.WriteLine("Press any key to exit!", Color.DarkRed);
            Console.ReadKey();
            return;
        }
        Console.WriteLine("VALORANT is Running!", Color.LimeGreen);
        PrepareAuthInst();
        var usr = Username.GetUsername(Auth);
        Console.WriteLine("Welcome: " + usr.GameName + "#" + usr.TagLine);
        await Task.Delay(1000);
        Console.Clear();
        PrintLogo();
        PrepareDiscordRPC();
        await PreparePresence();
        Console.ReadKey();
    }
    private void PrepareDiscordRPC()
    {
        Client = new("961899112440168458", autoEvents: false);
        Client.Logger = new ConsoleLogger { Level = LogLevel.Warning };
        Client.OnReady += (sender, e) => Console.WriteLine($"Discord is Ready! user {e.User.Username}");
        Client.OnPresenceUpdate += (sender, msg) =>
        {
            Console.Clear();
            PrintLogo();
            Console.WriteLine("Discord RPC has been updated!", Color.Green);
        };
        Client.Initialize();
    }
    private void PrintLogo()
    {
        Console.WriteLine($@"
 __      __     _        _____  _____   _____ 
 \ \    / /\   | |      |  __ \|  __ \ / ____|
  \ \  / /  \  | |      | |__) | |__) | |     
   \ \/ / /\ \ | |      |  _  /|  ___/| |     
    \  / ____ \| |____  | | \ \| |    | |____ 
     \/_/    \_\______| |_|  \_\_|     \_____|
       
  by CruSheR#2015 - https://github.com/Crv5heR
  {GetNameDyn()}
                                              ", Color.OrangeRed);
    }
    private string GetNameDyn()
    {
        if (Auth is null) return "Loading...";
        var usr = Username.GetUsername(Auth);
        return $"Welcome {usr.GameName}#{usr.TagLine}, Have a great day!";
    }
    private async Task PreparePresence()
    {
        var myTI = new CultureInfo("en-US", false).TextInfo;
        await Task.Run(async () =>
        {
            while (true)
            {
                var pre = new RichPresence
                {
                    Details = "Menus",
                    Assets = new Assets
                    {
                        LargeImageKey = "logo"
                    },
                    Party = new Party
                    {
                        ID = Presence?.privinfo.partyId,
                        Max = Presence?.privinfo.maxPartySize ?? 0,
                        Privacy = Party.PrivacySetting.Public,
                        Size = Presence?.privinfo.partySize ?? 0
                    }

                };
                Client?.Invoke();
                Presence = UserPresence.GetPresence().presences[0];
                if (Presence is not null)
                    if (!string.IsNullOrWhiteSpace(Presence.privinfo.matchMap))
                        (MapName, GameMode) = (Presence.privinfo.matchMap, Presence.privinfo.queueId);

                if (Presence is null || Presence.privinfo.sessionLoopState is "MENUS")
                {
                    if (IsNotValorantRunning) break;
                    if (Presence?.privinfo.partyAccessibility is "OPEN")
                        pre.State = Presence.privinfo.partyState is "MATCHMAKING" ?
                         $"Searching ({myTI.ToTitleCase(Presence.privinfo.queueId)})" :
                         $"Waiting ({myTI.ToTitleCase(Presence.privinfo.queueId)})";
                    else if (Presence is not null)
                        pre.State = Presence.privinfo.partyState is "MATCHMAKING" ?
                         $"Searching ({myTI.ToTitleCase(Presence.privinfo.queueId)})" :
                         $"Waiting ({myTI.ToTitleCase(Presence.privinfo.queueId)})";
                    SetPresence(pre);
                    await Task.Delay(7500);
                }
                else
                {
                    if (Presence.privinfo.provisioningFlow is "ShootingRange")
                        (GameMode, MapName) = ("Shooting Range", @"/Game/Maps/Poveglia/Range");
                    //One size fits all in game presence
                    pre.Details = $"Playing {myTI.ToTitleCase(GameMode ?? String.Empty)}  on {GetMapName(MapName ?? String.Empty)}";
                    pre.State = $"{Presence.privinfo.partyOwnerMatchScoreAllyTeam} - {Presence.privinfo.partyOwnerMatchScoreEnemyTeam}";
                    pre.Assets.LargeImageKey = GetMapName(MapName ?? String.Empty).ToLower().Replace(" ", "_");
                    pre.Assets.LargeImageText = GetMapName(MapName ?? String.Empty);
                    pre.Timestamps = new() { Start = null };
                }
                SetPresence(pre);
                await Task.Delay(10000);
            }
        });
    }
    private void SetPresence(RichPresence pre)
    {
        //CurrentPre = pre; //testing
        Client?.SetPresence(pre);
        Client?.Invoke();
        if (Presence?.privinfo.queueId is not null)
            Console.WriteLine($"Current Queue: {Presence.privinfo.queueId}", Color.LightGoldenrodYellow);
        Console.WriteLine($"Current Party Size: {Presence?.privinfo.partySize}", Color.LightGoldenrodYellow);
        if (!string.IsNullOrWhiteSpace(Presence?.privinfo.matchMap))
            Console.WriteLine($"Current Map: {GetMapName(Presence.privinfo.matchMap)}", Color.LightGoldenrodYellow);
    }
    public static string GetMapName(string mapid) => mapid switch
    {
        "/Game/Maps/Ascent/Ascent" => "Ascent",
        "/Game/Maps/Bonsai/Bonsai" => "Split",
        "/Game/Maps/Duality/Duality" => "Bind",
        "/Game/Maps/Port/Port" => "Icebox",
        "/Game/Maps/Triad/Triad" => "Haven",
        "/Game/Maps/Foxtrot/Foxtrot" => "Breeze",
        "/Game/Maps/Canyon/Canyon" => "Fracture",
        "/Game/Maps/Poveglia/Range" => "The Range",
        _ => mapid
    };
    public static string GetModeName(string mode) => mode switch
    {
        "/Game/GameModes/Bomb/BombGameMode.BombGameMode_C" => "Standard",
        "/Game/GameModes/Deathmatch/DeathmatchGameMode.DeathmatchGameMode_C" => "Deathmatch",
        "/Game/GameModes/GunGame/GunGameTeamsGameMode.GunGameTeamsGameMode_C" => "Escalation",
        "/Game/GameModes/OneForAll/OneForAll_GameMode.OneForAll_GameMode_C" => "Replication",
        "/Game/GameModes/QuickBomb/QuickBombGameMode.QuickBombGameMode_C" => "Spike Rush",
        "/Game/GameModes/ShootingRange/ShootingRangeGameMode.ShootingRangeGameMode_C" => "Shooting Range",
        _ => "Unknown Mode"
    };
}